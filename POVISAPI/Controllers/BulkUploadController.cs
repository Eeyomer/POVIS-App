using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using POVISAPI.Interfaces;
using POVISAPI.Models.System_Models;

namespace POVISAPI.Controllers
{
    /// <summary>Data-migration bulk upload for every module. MIS Personnel only.
    /// (Ordinances have their own endpoint: POST api/Ordinances/bulk-upload.)</summary>
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowFrontend")]
    [Authorize(Roles = "MISPersonnel")]
    public class BulkUploadController : Controller
    {
        private readonly IBulkImportService importService;
        public BulkUploadController(IBulkImportService importService) => this.importService = importService;

        [HttpGet]
        public IActionResult ListModules() => Ok(new APIResponse<object>
        {
            Success = true,
            Message = "Bulk upload modules retrieved successfully",
            Data = importService.ListModules()
        });

        [HttpGet("{module}/template")]
        public IActionResult GetTemplate(string module)
        {
            var bytes = importService.GetTemplate(module);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{module}-bulk-upload-template.xlsx");
        }

        [HttpPost("{module}")]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> Upload(string module, IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file was uploaded.");
            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Only .xlsx files are supported. Please use the provided template.");

            await using var stream = file.OpenReadStream();
            var result = await importService.ImportAsync(module, stream, User.Identity?.Name ?? "bulk-upload");

            if (result.Errors.Count > 0)
                return BadRequest(new APIResponse<object>
                {
                    Success = false,
                    Message = $"The file has {result.Errors.Count} error(s). Nothing was imported.",
                    Data = result
                });

            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Bulk upload completed successfully",
                Data = result
            });
        }
    }
}
