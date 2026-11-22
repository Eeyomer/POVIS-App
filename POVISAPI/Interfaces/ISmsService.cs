namespace POVISAPI.Interfaces
{
    public interface ISmsService
    {
        /// <summary>True when the API token and sender id are configured and sending is enabled.</summary>
        bool IsConfigured { get; }

        /// <summary>Sends a plain-text SMS through PhilSMS. Accepts 09XXXXXXXXX, 639XXXXXXXXX or +639XXXXXXXXX.</summary>
        /// <exception cref="InvalidOperationException">SMS is not configured, or PhilSMS rejected the message.</exception>
        /// <exception cref="ArgumentException">The number or message is invalid.</exception>
        Task SendAsync(string recipient, string message, CancellationToken ct = default);
    }
}
