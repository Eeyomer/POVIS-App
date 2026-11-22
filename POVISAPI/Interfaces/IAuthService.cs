using POVISAPI.Models.DTO;

namespace POVISAPI.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDTO> RegisterAsync(RegisterDTO dto);
    Task<AuthResponseDTO> LoginAsync(LoginDTO dto);
    Task<AuthResponseDTO> ChangePasswordAsync(Guid userId, ChangePasswordDTO dto);

    /// <summary>Verifies username + registered cellphone number, then texts a new 8-digit temporary password (which must be changed at next login).</summary>
    Task<ForgotPasswordResultDTO> ForgotPasswordAsync(ForgotPasswordDTO dto, string? ipAddress);
    Task<bool> AdminExistsAsync();
    Task<MyProfileDTO> GetProfileAsync(Guid userId);
}
