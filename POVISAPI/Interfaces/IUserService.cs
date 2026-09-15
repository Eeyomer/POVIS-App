using POVISAPI.Models.DTO;

namespace POVISAPI.Interfaces;

public interface IUserService
{
    Task<List<UserDTO>> GetAllAsync();
    Task<UserDTO> GetByIdAsync(Guid id);
    Task<UserDTO> CreateAsync(CreateUserDTO dto);
    Task<UserDTO> UpdateAsync(UpdateUserDTO dto);
    Task ResetPasswordAsync(Guid id, ResetPasswordDTO dto);
    Task DeleteAsync(Guid id);
}
