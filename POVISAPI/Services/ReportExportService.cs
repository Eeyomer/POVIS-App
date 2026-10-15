using POVISAPI.Interfaces;
using POVISAPI.Models.Entity.Enums;
using POVISAPI.Services.Reports;

namespace POVISAPI.Services
{
    public class ReportExportService : IReportExportService
    {
        private readonly IReportService _reports;
        public ReportExportService(IReportService reports) => _reports = reports;

        public async Task<ReportFile> ExportAsync(string report, string format, DateOnly from, DateOnly to, Guid? barangayId, Division? division)
        {
            var doc = report.ToLowerInvariant() switch
            {
                "overall" => ReportDocumentBuilder.Overall(await _reports.GetOrdinanceSummaryAsync(from, to)),
                "per-barangay" => ReportDocumentBuilder.PerBarangay(await _reports.GetPerBarangayAsync(from, to)),
                "seminar" => ReportDocumentBuilder.SeminarPerBarangay(await _reports.GetSeminarPerBarangayAsync(from, to, barangayId)),
                "incentives" => ReportDocumentBuilder.Incentives(await _reports.GetIncentivesAsync(from, to, division)),
                "enforcers" => ReportDocumentBuilder.Enforcers(await _reports.GetEnforcersAsync()),
                _ => throw new ArgumentException("Unknown report. Use overall, per-barangay, seminar, incentives or enforcers.")
            };

            return format.ToLowerInvariant() switch
            {
                "pdf" => new ReportFile(PdfReportRenderer.Render(doc), "application/pdf", $"{doc.FileBaseName}.pdf"),
                "xlsx" => new ReportFile(ExcelReportRenderer.Render(doc),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{doc.FileBaseName}.xlsx"),
                _ => throw new ArgumentException("format must be 'pdf' or 'xlsx'.")
            };
        }
    }
}
