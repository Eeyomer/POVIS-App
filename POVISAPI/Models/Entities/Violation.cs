using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Models.Entity;

/// <summary>
/// One apprehension / OVR ticket. This is the core record behind the
/// VIOLATORS, WARNING TICKET, and REACH OUT MINORS sheets — they share the
/// same columns and differ only by status/ordinance, so they are one table.
/// PAID, seminar, community service, counseling, and filed-case sheets hang
/// off this entity as child records.
/// </summary>
public class Violation : BaseEntity
{
    public Guid ViolatorId { get; set; }
    public Violator Violator { get; set; } = null!;

    /// <summary>OVR ticket number, e.g. "027620". Unique per ticket.</summary>
    public string OvrNumber { get; set; } = string.Empty;

    /// <summary>DATE OF SUBMISSION — when the ticket was submitted/encoded.</summary>
    public DateOnly DateOfSubmission { get; set; }

    /// <summary>Apprehending unit (BPD / PNP / CENRO).</summary>
    public Division Division { get; set; }

    /// <summary>Age at time of apprehension (AGE column — snapshot, not derived).</summary>
    public int? AgeAtApprehension { get; set; }

    // ----- Occurrence details -----
    public DateOnly DateOfOccurrence { get; set; }
    public TimeOnly? TimeOfOccurrence { get; set; }
    public string? PlaceOfOccurrence { get; set; }

    /// <summary>BARANGAY OF P.O — barangay where the violation occurred.
    /// Drives the PER BRGY ORDINANCES report.</summary>
    public Guid? BarangayOfOccurrenceId { get; set; }
    public Barangay? BarangayOfOccurrence { get; set; }

    public string? Witness { get; set; }
    public string? ApprehendingOfficer { get; set; }

    // ----- Ordinance violated -----
    public Guid OrdinanceId { get; set; }
    public Ordinance Ordinance { get; set; } = null!;

    /// <summary>Specific section violated when applicable (e.g. Ord. 19 Sec. 24 littering).</summary>
    public Guid? OrdinanceSectionId { get; set; }
    public OrdinanceSection? OrdinanceSection { get; set; }

    /// <summary>1st/2nd/3rd/4th offense of THIS violator against THIS ordinance,
    /// computed at ticketing time (SECOND OFFENSE / OFFENSE columns).</summary>
    public OffenseLevel OffenseLevel { get; set; } = OffenseLevel.First;

    /// <summary>True when this ticket is a warning only (WARNING TICKET sheet).</summary>
    public bool IsWarningTicket { get; set; }

    public ViolationStatus Status { get; set; } = ViolationStatus.Pending;

    /// <summary>REMARKS / REMARKS II — free-text notes ("Two violations in one OVR ticket",
    /// "Paid at CENRO PC No. ...", "NEW UPDATE", etc.).</summary>
    public string? Remarks { get; set; }
    public string? Remarks2 { get; set; }

    // ----- Child records -----
    public Payment? Payment { get; set; }
    public FiledCase? FiledCase { get; set; }
    public ICollection<SeminarRecord> SeminarRecords { get; set; } = new List<SeminarRecord>();
    public ICollection<CommunityServiceRecord> CommunityServiceRecords { get; set; } = new List<CommunityServiceRecord>();
    public ICollection<CounselingRecord> CounselingRecords { get; set; } = new List<CounselingRecord>();
}
