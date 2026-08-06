using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Models.Entity;

/// <summary>
/// Cessation / intervention seminar attendance in lieu of (or in addition to) a fine.
/// Covers the SMOKING SEMINAR and SEMINAR VAPING sheets via SeminarType, and can
/// also record the solid-waste seminar required by Ord. 01 S.2018.
/// Key columns: CENRO SEMINAR SCHEDULE, COMPLETED CESSATION SEMINAR.
/// </summary>
public class SeminarRecord : BaseEntity
{
    public Guid ViolationId { get; set; }
    public Violation Violation { get; set; } = null!;

    public SeminarType SeminarType { get; set; }

    /// <summary>CENRO-assigned schedule, e.g. "June 2, 2026".</summary>
    public DateOnly? ScheduledDate { get; set; }

    /// <summary>Required days per the penalty (1, 2.5, 5, 15...). Decimal because
    /// Ord. 07 S.2019 prescribes 2.5 days on the 2nd offense.</summary>
    public decimal? RequiredDays { get; set; }

    public ComplianceStatus Status { get; set; } = ComplianceStatus.Scheduled;

    public DateOnly? DateCompleted { get; set; }

    public string? Remarks { get; set; }
}
