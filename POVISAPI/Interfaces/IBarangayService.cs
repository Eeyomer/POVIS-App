using POVISAPI.Models.DTO;

namespace POVISAPI.Interfaces
{
    public interface IBarangayService
    {
        Task<List<BarangayDTO>> GetAllAsync(bool includeInactive = false);
        Task<BarangayDTO> GetByIdAsync(string id);
        Task<BarangayDTO> CreateAsync(AddBarangayDTO dto);
        Task<BarangayDTO> UpdateAsync(UpdateBarangayDTO dto);
        Task DeleteAsync(string id);
    }
}
