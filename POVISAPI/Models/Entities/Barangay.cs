namespace POVISAPI.Models.Entity;

/// <summary>
/// Lookup table for the 30 Pasig barangays + special areas (e.g. Pasig MegaMarket),
/// grouped by district. Source: PER BRGY ORDINANCES sheet.
/// Used both for the violator's home barangay and the barangay of occurrence,
/// which drives the per-barangay ordinance report.
/// </summary>
public class Barangay : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>1 or 2. Null for special areas like Pasig MegaMarket.</summary>
    public int? District { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Violation> ViolationsOccurred { get; set; } = new List<Violation>();
    public ICollection<Violator> Residents { get; set; } = new List<Violator>();
}
