namespace POVISAPI.Models.System_Models
{
    public class PhilSmsSettings
    {
        public bool Enabled { get; set; } = true;
        public string BaseUrl { get; set; } = "https://dashboard.philsms.com/api/v3/sms/send";
        public string ApiKey { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
    }
}
