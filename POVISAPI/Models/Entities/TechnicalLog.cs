namespace POVISAPI.Models.Entity;

/// <summary>Per-request technical log entry, viewable by MIS Personnel.</summary>
public class TechnicalLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long DurationMs { get; set; }
    public string? Username { get; set; }
    public string? IpAddress { get; set; }
}
