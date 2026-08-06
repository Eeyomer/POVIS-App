using POVISAPI.Models.DTO;

namespace POVISAPI.Interfaces
{
    public interface IViolationService
    {
        Task<PagedResult<ViolationDTO>> GetAllAsync(ViolationFilterDTO filter);
        Task<ViolationDTO> GetByIdAsync(string id);
        Task<ViolationDTO> CreateAsync(AddViolationDTO dto);
        Task<ViolationDTO> UpdateAsync(UpdateViolationDTO dto);
        Task DeleteAsync(string id);

        /// <summary>Resolves the fine/penalty applicable to a violation from the
        /// ordinance penalty schedule, using the violation's snapshotted offense level.</summary>
        Task<ApplicablePenaltyDTO> GetApplicablePenaltyAsync(string id);
    }
}
