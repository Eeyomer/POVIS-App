using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Models.Entity;

/// <summary>
/// Case filed with the prosecutor for a violation that wasn't settled.
/// Source: FILED CASE sheet — batches dated per filing (e.g. "FILED CASE – June 22, 2026"),
/// with REMARKS/NPS DOCKET NUMBER, STATUS, and DISMISSED columns.
/// </summary>
public class FiledCase : BaseEntity
{
    public Guid ViolationId { get; set; }
    public Violation Violation { get; set; } = null!;

    /// <summary>The batch filing date shown as the section header in the sheet.</summary>
    public DateOnly DateFiled { get; set; }

    /// <summary>National Prosecution Service docket number, when assigned.</summary>
    public string? NpsDocketNumber { get; set; }

    public CaseStatus Status { get; set; } = CaseStatus.Filed;

    /// <summary>Set when dismissed or closed (e.g. "CASE CLOSED PAID June 30, 2026").</summary>
    public DateOnly? DateResolved { get; set; }

    /// <summary>How it was resolved: dismissed, paid after prosecution decision, etc.</summary>
    public string? ResolutionRemarks { get; set; }

    public string? Remarks { get; set; }
}
