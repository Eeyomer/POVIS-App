using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using POVISAPI.Interfaces;
using POVISAPI.Models.System_Models;

namespace POVISAPI.Services
{
    /// <summary>
    /// Sends SMS through PhilSMS. The API token and sender id come from configuration only
    /// (environment variables PhilSms__ApiKey and PhilSms__SenderId) and are never logged.
    /// </summary>
    public class SmsService : ISmsService
    {
        public const int MaxMessageLength = 480;

        private readonly PhilSmsSettings settings;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<SmsService> logger;

        public SmsService(IOptions<PhilSmsSettings> options, IHttpClientFactory httpClientFactory, ILogger<SmsService> logger)
        {
            settings = options.Value;
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
        }

        public bool IsConfigured =>
            settings.Enabled && !string.IsNullOrWhiteSpace(settings.ApiKey) && !string.IsNullOrWhiteSpace(settings.SenderId);

        /// <summary>Normalizes a Philippine mobile number to PhilSMS format (639XXXXXXXXX), or null when it is not one.</summary>
        public static string? NormalizeRecipient(string? number)
        {
            var digits = Regex.Replace(number ?? string.Empty, @"[\s\-()+]", "");
            if (!Regex.IsMatch(digits, @"^\d+$")) return null;
            if (digits.StartsWith("09") && digits.Length == 11) digits = "63" + digits[1..];
            else if (digits.StartsWith("9") && digits.Length == 10) digits = "63" + digits;
            return Regex.IsMatch(digits, @"^639\d{9}$") ? digits : null;
        }

        public async Task SendAsync(string recipient, string message, CancellationToken ct = default)
        {
            var to = NormalizeRecipient(recipient)
                ?? throw new ArgumentException("Enter a valid Philippine mobile number (e.g. 09171234567).");
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message is required.");
            if (message.Length > MaxMessageLength)
                throw new ArgumentException($"Message cannot exceed {MaxMessageLength} characters.");
            if (!IsConfigured)
                throw new InvalidOperationException("SMS is not configured on the server.");

            using var request = new HttpRequestMessage(HttpMethod.Post, settings.BaseUrl)
            {
                Content = JsonContent.Create(new
                {
                    recipient = to,
                    sender_id = settings.SenderId,
                    type = "plain",
                    message = message.Trim()
                })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response;
            string body;
            try
            {
                var client = httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(20);
                response = await client.SendAsync(request, ct);
                body = await response.Content.ReadAsStringAsync(ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogError(ex, "PhilSMS request failed to complete.");
                throw new InvalidOperationException("The SMS provider could not be reached. Please try again.");
            }

            // PhilSMS answers {"status":"success"|"error","message":"..."}; the HTTP code alone is not enough.
            string? status = null, providerMessage = null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("status", out var s)) status = s.GetString();
                    if (doc.RootElement.TryGetProperty("message", out var m)) providerMessage = m.ValueKind == JsonValueKind.String ? m.GetString() : m.ToString();
                }
            }
            catch (JsonException) { /* non-JSON body: treated as a failure below */ }

            if (response.IsSuccessStatusCode && string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("SMS sent to {Recipient}.", MaskNumber(to));
                return;
            }

            logger.LogWarning("PhilSMS rejected a message to {Recipient}: HTTP {StatusCode}, {Reason}",
                MaskNumber(to), (int)response.StatusCode, providerMessage);
            throw new InvalidOperationException("The SMS could not be sent. Please check the number and try again.");
        }

        private static string MaskNumber(string number) => number.Length > 4 ? new string('*', number.Length - 4) + number[^4..] : "****";
    }
}
