using POVISAPI.Models.DTO;

namespace POVISAPI.Interfaces
{
    public interface IOrdinanceService
    {
        Task<List<OrdinanceListDTO>> GetAllAsync(bool includeInactive = false);
        Task<OrdinanceDTO> GetByIdAsync(string id);
        Task<OrdinanceDTO> CreateAsync(AddOrdinanceDTO dto);
        Task<OrdinanceDTO> UpdateAsync(UpdateOrdinanceDTO dto);
        /// <summary>Deletes the ordinance, or archives (deactivates) it when other records reference it. Returns true if archived.</summary>
        Task<bool> DeleteAsync(string id);

        Task<OrdinanceSectionDTO> AddSectionAsync(string ordinanceId, AddOrdinanceSectionDTO dto);
        Task<OrdinanceSectionDTO> UpdateSectionAsync(string ordinanceId, string sectionId, AddOrdinanceSectionDTO dto);
        Task RemoveSectionAsync(string ordinanceId, string sectionId);

        Task<OrdinancePenaltyDTO> AddPenaltyAsync(string ordinanceId, AddOrdinancePenaltyDTO dto);
        Task<OrdinancePenaltyDTO> UpdatePenaltyAsync(string ordinanceId, string penaltyId, UpdateOrdinancePenaltyDTO dto);
        Task RemovePenaltyAsync(string ordinanceId, string penaltyId);
    }
}
