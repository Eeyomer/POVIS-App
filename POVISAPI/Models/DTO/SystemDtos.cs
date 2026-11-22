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

    /// <summary>Incentive percentage (10 = 10%) given to apprehending officers on payments recorded from now on.
    /// Read-only here: it is changed through PUT api/settings/incentive-rate, never by the modal-forms toggle.</summary>
    public decimal IncentiveRate { get; set; }
}

public class IncentiveRateDTO
{
    [Range(0, 100, ErrorMessage = "Incentive rate must be between 0 and 100")]
    public decimal Rate { get; set; }
}
