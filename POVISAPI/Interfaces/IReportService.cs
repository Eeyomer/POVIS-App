using POVISAPI.Models.DTO;
using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Interfaces
{
    public interface IReportService
    {
        Task<OrdinanceSummaryReportDTO> GetOrdinanceSummaryAsync(DateOnly from, DateOnly to);
        Task<PerBarangayReportDTO> GetPerBarangayAsync(DateOnly from, DateOnly to);
        Task<SeminarBarangayReportDTO> GetSeminarPerBarangayAsync(DateOnly from, DateOnly to, Guid? barangayId);
        Task<IncentiveReportDTO> GetIncentivesAsync(DateOnly from, DateOnly to, Division? division);
        Task<EnforcerListDTO> GetEnforcersAsync();
    }
}
