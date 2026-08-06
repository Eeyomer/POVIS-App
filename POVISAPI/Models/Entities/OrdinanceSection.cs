namespace POVISAPI.Models.Entity;

/// <summary>
/// Optional subdivision of an ordinance that carries its own penalty schedule.
/// Examples from the docx:
///   - Ord. 19 S.2025: Sections 21–36 each have distinct fines
///   - Ord. 20 S.2012 (Sanitation Code): Section 8 / Section 9, Chapter III
///   - Ord. 74 S.2023: 8.1 (minor), 8.2 (establishments), 8.3 (other individuals)
///   - Ord. 26 S.2024 (Veterinary Code): Sections 38–43 vs. Section 38 (3.2)
/// Ordinances with a single flat penalty (e.g. Ord. 18 S.1992) simply have no sections.
/// </summary>
public class OrdinanceSection : BaseEntity
{
    public Guid OrdinanceId { get; set; }
    public Ordinance Ordinance { get; set; } = null!;

    /// <summary>e.g. "Section 21", "Section 38 (3.2)", "8.2", "Section 8, Chapter III".</summary>
    public string SectionCode { get; set; } = string.Empty;

    /// <summary>What the section prohibits, e.g. "Littering of cigarette butts".</summary>
    public string? Description { get; set; }

    public ICollection<OrdinancePenalty> Penalties { get; set; } = new List<OrdinancePenalty>();
    public ICollection<Violation> Violations { get; set; } = new List<Violation>();
}
