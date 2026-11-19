using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Models.DTO;

public class RegisterDTO
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public Role Role { get; set; }
}

public class LoginDTO
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDTO
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public Role Role { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool MustChangePassword { get; set; }
}

/// <summary>The signed-in user's own display profile (name and photo for the sidebar).</summary>
public class MyProfileDTO
{
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    /// <summary>Base64 data URL, or null when no photo was uploaded.</summary>
    public string? Photo { get; set; }
}
