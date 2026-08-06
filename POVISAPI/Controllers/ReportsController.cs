using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using POVISAPI.Interfaces;
using POVISAPI.Models.System_Models;

namespace POVISAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowFrontend")]
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IReportService reportService;
        public ReportsController(IReportService reportService)
        {
            this.reportService = reportService;
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
