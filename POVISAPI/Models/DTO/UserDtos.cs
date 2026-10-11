using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Models.DTO;

public class UserDTO
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public Role Role { get; set; }
    public bool IsActive { get; set; }
    public string? DeputationNumber { get; set; }
    public bool MustChangePassword { get; set; }

    /// <summary>Only populated in the response to create, and shown to MIS Personnel once.</summary>
    public string? TemporaryPassword { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserDTO
{
    public string Username { get; set; } = string.Empty;
    public Role Role { get; set; }
    public string? DeputationNumber { get; set; }
}

public class UpdateUserDTO
{
    public Guid Id { get; set; }
    public Role Role { get; set; }
    public bool IsActive { get; set; }
    public string? DeputationNumber { get; set; }
}

public class ChangePasswordDTO
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
