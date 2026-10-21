using System.ComponentModel.DataAnnotations;

namespace POVISAPI.Models.DTO;

public class TechnicalLogDTO
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long DurationMs { get; set; }
    public string? Username { get; set; }
    public string? IpAddress { get; set; }
}

public class TechnicalLogFilterDTO
{
    public string? Search { get; set; }
    public string? Method { get; set; }
    public int? StatusCode { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1")]
    public int Page { get; set; } = 1;

    [Range(1, 200, ErrorMessage = "Page size must be between 1 and 200")]
    public int PageSize { get; set; } = 50;
}

public class SystemHealthDTO
{
    public bool DatabaseConnected { get; set; }
    public DateTime ServerTimeUtc { get; set; }
    public int TotalUsers { get; set; }
    public int TotalViolators { get; set; }
    public int TotalViolations { get; set; }
    public int TotalTechnicalLogs { get; set; }
}

public class AppSettingsDTO
{
    /// <summary>When true, create/edit/view panels open in a modal for every user.</summary>
    public bool ModalForms { get; set; }
}
