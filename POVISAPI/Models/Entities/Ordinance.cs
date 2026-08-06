namespace POVISAPI.Models.Entity;

/// <summary>
/// A city ordinance, e.g. "City Ordinance No. 19, Series of 2025 —
/// Pasig City Comprehensive Smoke-Free Environment Ordinance of 2025".
/// Source: ORDINANCE-PENALTIES-MODULE.docx (21 ordinances) and the
/// CITY ORDINANCE VIOLATED / CITY ORDINANCE NAME columns in the workbook.
/// </summary>
public class Ordinance : BaseEntity
{
    /// <summary>Ordinance number only, e.g. "19", "07", "PCG-60" (for executive orders).</summary>
    public string OrdinanceNumber { get; set; } = string.Empty;

    /// <summary>Year of the series, e.g. 2025.</summary>
    public int Series { get; set; }

    /// <summary>Full official title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Short display name used in tickets/reports, e.g. "No Smoking in Public Places".</summary>
    public string ShortName { get; set; } = string.Empty;

    /// <summary>True for executive orders like E.O. PCG-60 S.2022 (illegal firecracker selling).</summary>
    public bool IsExecutiveOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<OrdinanceSection> Sections { get; set; } = new List<OrdinanceSection>();
    public ICollection<OrdinancePenalty> Penalties { get; set; } = new List<OrdinancePenalty>();
    public ICollection<Violation> Violations { get; set; } = new List<Violation>();
}
