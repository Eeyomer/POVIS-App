using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Models.Entity;

/// <summary>
/// Compulsory counseling / assessment intervention under the Reach Out Minors
/// ordinances (Ord. 04 S.2016, Ord. 74 S.2023). The REACH OUT MINORS sheet rows
/// are stored as Violations; this child record tracks the counseling itself
/// (the MONTHLY report counts "17 For Counseling (PENDING)").
/// </summary>
public class CounselingRecord : BaseEntity
{
    public Guid ViolationId { get; set; }
    public Violation Violation { get; set; } = null!;

    /// <summary>1st offense: counseling; 2nd offense: assessment + counseling + intervention.</summary>
    public string? InterventionType { get; set; }

    /// <summary>e.g. Barangay Council conducting the counseling.</summary>
    public string? ConductedBy { get; set; }

    public DateOnly? ScheduledDate { get; set; }
    public DateOnly? DateCompleted { get; set; }

    public ComplianceStatus Status { get; set; } = ComplianceStatus.Scheduled;

    /// <summary>True when a parent/guardian attended (parental liability under Ord. 04 Sec. 28).</summary>
    public bool ParentOrGuardianPresent { get; set; }

    public string? Remarks { get; set; }
}
