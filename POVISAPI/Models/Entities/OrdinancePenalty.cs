using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Models.Entity;

/// <summary>
/// One row of a penalty schedule: what applies at a given offense level,
/// for a given ordinance (and optionally a specific section and offender type).
/// The docx shows penalties are a mix of fines, seminar days, community service hours,
/// imprisonment, and business-permit actions — sometimes "or", sometimes "and" —
/// so the amounts are kept in separate nullable columns plus a raw-text fallback.
/// </summary>
public class OrdinancePenalty : BaseEntity
{
    public Guid OrdinanceId { get; set; }
    public Ordinance Ordinance { get; set; } = null!;

    /// <summary>Null when the ordinance has one flat penalty schedule.</summary>
    public Guid? OrdinanceSectionId { get; set; }
    public OrdinanceSection? OrdinanceSection { get; set; }

    public OffenseLevel OffenseLevel { get; set; }

    /// <summary>Null for ordinances that don't split by offender type (most of them).
    /// Ord. 22 S.2002 (toy guns) and Ord. 74 S.2023 (minors) do.</summary>
    public OffenderType? OffenderType { get; set; }

    /// <summary>Fine in PHP. Null when the penalty is non-monetary
    /// (e.g. written warning, compulsory counseling).</summary>
    public decimal? FineAmount { get; set; }

    /// <summary>Days of intervention seminar (e.g. 5 days Tobacco Intervention Seminar).</summary>
    public int? SeminarDays { get; set; }

    /// <summary>Hours of community service (e.g. 4/8/16 hrs under Ord. 54–56 S.2017).</summary>
    public int? CommunityServiceHours { get; set; }

    /// <summary>Days of community service (e.g. 15 days under Ord. 07 S.2019 3rd offense).</summary>
    public int? CommunityServiceDays { get; set; }

    public bool RequiresCounseling { get; set; }

    /// <summary>True when imprisonment is a possible penalty; the term goes in ImprisonmentTerm
    /// as text since it varies wildly ("15 days", "not less than 1 month...", "6 months").</summary>
    public bool HasImprisonment { get; set; }
    public string? ImprisonmentTerm { get; set; }

    public BusinessPermitAction BusinessPermitAction { get; set; } = BusinessPermitAction.None;

    /// <summary>True when fine and the alternative penalty are conjunctive ("and"),
    /// false when the violator can choose ("or"), per the discretion wording in the docx.</summary>
    public bool IsConjunctive { get; set; }

    /// <summary>Verbatim penalty text from the ordinance for display/printing on the OVR.</summary>
    public string PenaltyText { get; set; } = string.Empty;
}
