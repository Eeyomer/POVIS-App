using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Models.Entity;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; }
    public bool IsActive { get; set; } = true;

    public string? FullName { get; set; }
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public MaritalStatus? MaritalStatus { get; set; }

    /// <summary>Profile photo as a base64 data URL (JPEG or PNG).</summary>
    public string? Photo { get; set; }

    /// <summary>True while the user is on a system-generated temporary password and must set their own.</summary>
    public bool MustChangePassword { get; set; }

    /// <summary>Deputation number issued to an Apprehending Officer. Null for other roles.</summary>
    public string? DeputationNumber { get; set; }
}
