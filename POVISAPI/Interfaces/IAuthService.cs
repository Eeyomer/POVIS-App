using POVISAPI.Models.DTO;

namespace POVISAPI.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDTO> RegisterAsync(RegisterDTO dto);
    Task<AuthResponseDTO> LoginAsync(LoginDTO dto);
    Task<AuthResponseDTO> ChangePasswordAsync(Guid userId, ChangePasswordDTO dto);
    Task<bool> AdminExistsAsync();
}
