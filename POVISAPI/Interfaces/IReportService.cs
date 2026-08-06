using POVISAPI.Models.DTO;

namespace POVISAPI.Interfaces
{
    public interface IReportService
    {
        Task<OrdinanceSummaryReportDTO> GetOrdinanceSummaryAsync(DateOnly from, DateOnly to);
        Task<PerBarangayReportDTO> GetPerBarangayAsync(DateOnly from, DateOnly to);
    }
}
