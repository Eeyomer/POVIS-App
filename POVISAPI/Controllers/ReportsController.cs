using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using POVISAPI.Interfaces;
using POVISAPI.Models.Entity.Enums;
using POVISAPI.Models.System_Models;

namespace POVISAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowFrontend")]
    [Authorize(Roles = "MISPersonnel,OVRSupervisor")]
    public class ReportsController : Controller
    {
        private readonly IReportService reportService;
        private readonly IReportExportService exportService;
        public ReportsController(IReportService reportService, IReportExportService exportService)
        {
            this.reportService = reportService;
            this.exportService = exportService;
        }

        /// <summary>
        /// Downloadable PDF / Excel version of a report, laid out like the POVIS.xlsx sample (logos and headings included).
        /// report: overall | per-barangay | seminar | incentives | enforcers. format: pdf | xlsx.
        /// barangayId only applies to "seminar" (omit for all barangays); division only to "incentives" (omit for all).
        /// </summary>
        [HttpGet("export/{report}")]
        public async Task<IActionResult> Export(string report, [FromQuery] string format, [FromQuery] DateOnly from, [FromQuery] DateOnly to,
            [FromQuery] Guid? barangayId, [FromQuery] Division? division, [FromQuery] string? officerIds)
        {
            if (to < from)
                throw new ArgumentException("'to' must be on or after 'from'.");

            // Comma-separated officer ids (apprehension report only).
            var officers = (officerIds ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Guid.TryParse(s, out var g) ? g : throw new ArgumentException("Invalid officer id."))
                .ToList();

            var file = await exportService.ExportAsync(report, format, from, to, barangayId, division, officers);
            return File(file.Content, file.ContentType, file.FileName);
        }

        /// <summary>MONTHLY sheet equivalent: pass the first and last day of the month.</summary>
        [HttpGet("ordinance-summary")]
        public async Task<IActionResult> GetOrdinanceSummary([FromQuery] DateOnly from, [FromQuery] DateOnly to)
        {
            if (to < from)
                throw new ArgumentException("'to' must be on or after 'from'.");

            var report = await reportService.GetOrdinanceSummaryAsync(from, to);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Ordinance summary report retrieved successfully",
                Data = report
            });
        }

        /// <summary>SEMI sheet: convenience endpoint for Jan–Jun / Jul–Dec of a year.</summary>
        [HttpGet("semi-annual/{year:int}/{half:int}")]
        public async Task<IActionResult> GetSemiAnnual(int year, int half)
        {
            if (half is not (1 or 2))
                throw new ArgumentException("half must be 1 or 2.");

            var from = new DateOnly(year, half == 1 ? 1 : 7, 1);
            var to = half == 1 ? new DateOnly(year, 6, 30) : new DateOnly(year, 12, 31);

            var report = await reportService.GetOrdinanceSummaryAsync(from, to);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Semi-annual report retrieved successfully",
                Data = report
            });
        }

        /// <summary>Executive summary for the dashboard: totals, collection, cases, top barangays and ordinances.</summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var summary = await reportService.GetDashboardSummaryAsync();
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Dashboard summary retrieved successfully",
                Data = summary
            });
        }

        /// <summary>PER BRGY ORDINANCES sheet: barangay x ordinance count matrix.</summary>
        [HttpGet("per-barangay")]
        public async Task<IActionResult> GetPerBarangay([FromQuery] DateOnly from, [FromQuery] DateOnly to)
        {
            if (to < from)
                throw new ArgumentException("'to' must be on or after 'from'.");

            var report = await reportService.GetPerBarangayAsync(from, to);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Per-barangay report retrieved successfully",
                Data = report
            });
        }
    }
}
