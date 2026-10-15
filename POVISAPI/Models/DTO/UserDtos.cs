using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Models.DTO;

public class UserDTO
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public Role Role { get; set; }
    public bool IsActive { get; set; }
    public string? FullName { get; set; }
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public MaritalStatus? MaritalStatus { get; set; }
    public string? Photo { get; set; }
    public string? DeputationNumber { get; set; }
    public bool MustChangePassword { get; set; }

    /// <summary>Only populated in the response to create, and shown to MIS Personnel once.</summary>
    public string? TemporaryPassword { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserDTO
{
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public MaritalStatus? MaritalStatus { get; set; }

    /// <summary>Base64 data URL (JPEG or PNG).</summary>
    public string? Photo { get; set; }
    public Role Role { get; set; }
    public string? DeputationNumber { get; set; }
}

public class UpdateUserDTO
{
    public Guid Id { get; set; }
    public Role Role { get; set; }
    public bool IsActive { get; set; }
    public string? FullName { get; set; }
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public MaritalStatus? MaritalStatus { get; set; }

    /// <summary>Null keeps the current photo, an empty string removes it, a base64 data URL replaces it.</summary>
    public string? Photo { get; set; }
    public string? DeputationNumber { get; set; }
}

public class ChangePasswordDTO
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
