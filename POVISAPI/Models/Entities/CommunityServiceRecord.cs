using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Models.Entity;

/// <summary>
/// Community service rendered in lieu of a fine (Ord. 54/55/56 S.2017 prescribe
/// 4/8/16 hours; Ord. 07 S.2019 3rd offense prescribes 15 days).
/// Source: COMMUNITY SERVICE sheet (same base columns + STATUS).
/// </summary>
public class CommunityServiceRecord : BaseEntity
{
    public Guid ViolationId { get; set; }
    public Violation Violation { get; set; } = null!;

    public decimal? RequiredHours { get; set; }
    public int? RequiredDays { get; set; }

    public DateOnly? ScheduledDate { get; set; }
    public DateOnly? DateCompleted { get; set; }

    public ComplianceStatus Status { get; set; } = ComplianceStatus.Scheduled;

    /// <summary>Where the service was rendered / supervising office.</summary>
    public string? AssignedLocation { get; set; }

    public string? Remarks { get; set; }
}
