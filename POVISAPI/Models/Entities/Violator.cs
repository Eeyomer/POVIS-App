using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Models.Entity;

/// <summary>
/// The apprehended person. One violator can have many violations, which is how
/// 2nd/3rd offenses are detected (SECOND OFFENSE column in VIOLATORS / PAID sheets).
/// Source columns: LAST NAME → CONTACT # across all sheets.
/// </summary>
public class Violator : BaseEntity
{
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string? Suffix { get; set; }

    public Gender Gender { get; set; }

    /// <summary>Nullable — many rows only have age at apprehension (stored on Violation).</summary>
    public DateOnly? Birthday { get; set; }

    /// <summary>Street address (WARNING TICKET splits street/barangay/city; keep street here).</summary>
    public string? Address { get; set; }

    /// <summary>Home barangay when the violator resides in Pasig; null otherwise.</summary>
    public Guid? BarangayId { get; set; }
    public Barangay? Barangay { get; set; }

    /// <summary>City of residence for non-Pasig residents (Marikina, Taytay, Antipolo, etc.).</summary>
    public string? City { get; set; }

    /// <summary>PRESENTED VALID ID, e.g. "DRIVER'S LICENSE", "NATIONAL ID", "VACCINE CARD".</summary>
    public string? PresentedValidId { get; set; }

    public string? ContactNumber { get; set; }

    public ICollection<Violation> Violations { get; set; } = new List<Violation>();
}
