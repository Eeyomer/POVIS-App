using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Interfaces
{
    public record ReportFile(byte[] Content, string ContentType, string FileName);

    public interface IReportExportService
    {
        /// <param name="report">overall | per-barangay | seminar | incentives | enforcers</param>
        /// <param name="format">pdf | xlsx</param>
        Task<ReportFile> ExportAsync(string report, string format, DateOnly from, DateOnly to, Guid? barangayId, Division? division);
    }
}
