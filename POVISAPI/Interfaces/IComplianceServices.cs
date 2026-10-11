using POVISAPI.Models.DTO;
using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Interfaces
{
    public interface ISeminarRecordService
    {
        Task<PagedResult<SeminarRecordDTO>> GetAllAsync(SeminarType? type, ComplianceStatus? status, int page = 1, int pageSize = 20, string? search = null);
        Task<SeminarRecordDTO> GetByIdAsync(string id);
        Task<SeminarRecordDTO> CreateAsync(AddSeminarRecordDTO dto);
        Task<SeminarRecordDTO> UpdateAsync(UpdateSeminarRecordDTO dto);
        Task DeleteAsync(string id);
    }

    public interface ICommunityServiceRecordService
    {
        Task<PagedResult<CommunityServiceDTO>> GetAllAsync(ComplianceStatus? status, int page = 1, int pageSize = 20, string? search = null);
        Task<CommunityServiceDTO> GetByIdAsync(string id);
        Task<CommunityServiceDTO> CreateAsync(AddCommunityServiceDTO dto);
        Task<CommunityServiceDTO> UpdateAsync(UpdateCommunityServiceDTO dto);
        Task DeleteAsync(string id);
    }

    public interface ICounselingRecordService
    {
        Task<PagedResult<CounselingDTO>> GetAllAsync(ComplianceStatus? status, int page = 1, int pageSize = 20, string? search = null);
        Task<CounselingDTO> GetByIdAsync(string id);
        Task<CounselingDTO> CreateAsync(AddCounselingDTO dto);
        Task<CounselingDTO> UpdateAsync(UpdateCounselingDTO dto);
        Task DeleteAsync(string id);
    }
}
