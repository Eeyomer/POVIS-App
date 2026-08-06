using POVISAPI.Models.DTO;
using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Interfaces
{
    public interface IFiledCaseService
    {
        Task<PagedResult<FiledCaseDTO>> GetAllAsync(CaseStatus? status, DateOnly? from, DateOnly? to, int page = 1, int pageSize = 20);
        Task<FiledCaseDTO> GetByIdAsync(string id);
        Task<FiledCaseDTO> CreateAsync(AddFiledCaseDTO dto);
        Task<FiledCaseDTO> UpdateAsync(UpdateFiledCaseDTO dto);
        Task DeleteAsync(string id);
    }
}
