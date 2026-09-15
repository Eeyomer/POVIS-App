using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Models.DTO;

public class UserDTO
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public Role Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserDTO
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public Role Role { get; set; }
}

public class UpdateUserDTO
{
    public Guid Id { get; set; }
    public Role Role { get; set; }
    public bool IsActive { get; set; }
}

public class ResetPasswordDTO
{
    public string NewPassword { get; set; } = string.Empty;
}
