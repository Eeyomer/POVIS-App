using POVISAPI.Common;

namespace POVISAPI.Models.System_Models
{
    public class APIResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public string? ErrorCode { get; set; }
        public DateTime TimeStamp { get; set; } = Clock.PhNow;
    }
}
