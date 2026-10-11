using POVISAPI.Models.DTO;

namespace POVISAPI.Interfaces;

public interface IUserService
{
    Task<List<UserDTO>> GetAllAsync();
    Task<UserDTO> GetByIdAsync(Guid id);
    Task<UserDTO> CreateAsync(CreateUserDTO dto);
    Task<UserDTO> UpdateAsync(UpdateUserDTO dto);
    /// <summary>Sets a new system-generated temporary password and returns it (shown once to MIS Personnel).</summary>
    Task<string> ResetPasswordAsync(Guid id);
    Task DeleteAsync(Guid id);
}
