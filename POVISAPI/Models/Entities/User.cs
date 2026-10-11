using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Models.Entity;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Deputation number issued to an Apprehending Officer. Null for other roles.</summary>
    public string? DeputationNumber { get; set; }
}
