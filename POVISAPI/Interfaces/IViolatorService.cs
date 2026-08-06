using POVISAPI.Models.DTO;

namespace POVISAPI.Interfaces
{
    public interface IViolatorService
    {
        Task<PagedResult<ViolatorDTO>> SearchAsync(string? search, int page = 1, int pageSize = 20);
        Task<ViolatorDTO> GetByIdAsync(string id);
        Task<List<ViolatorHistoryDTO>> GetHistoryAsync(string id);
        Task<ViolatorDTO> CreateAsync(AddViolatorDTO dto);
        Task<ViolatorDTO> UpdateAsync(UpdateViolatorDTO dto);
        Task DeleteAsync(string id);
    }
}
