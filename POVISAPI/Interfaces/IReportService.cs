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
        /// <summary>Violations in the period; limited to those recorded by the given officers when any are passed.</summary>
        Task<ApprehensionReportDTO> GetApprehensionAsync(DateOnly from, DateOnly to, List<Guid>? officerIds);
        Task<DashboardSummaryDTO> GetDashboardSummaryAsync();
    }
}
