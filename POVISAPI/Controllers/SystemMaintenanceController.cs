using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using POVISAPI.Interfaces;
using POVISAPI.Models.DTO;
using POVISAPI.Models.System_Models;

namespace POVISAPI.Controllers
{
    /// <summary>System maintenance, backups, restoration, and technical logs — MIS Personnel only.</summary>
    [ApiController]
    [Route("api/system")]
    [EnableCors("AllowFrontend")]
    [Authorize(Roles = "MISPersonnel")]
    public class SystemMaintenanceController : Controller
    {
        private readonly ISystemMaintenanceService systemService;
        public SystemMaintenanceController(ISystemMaintenanceService systemService)
        {
            this.systemService = systemService;
        }

        [HttpGet("health")]
        public async Task<IActionResult> GetHealth()
        {
            var health = await systemService.GetHealthAsync();
            return Ok(new APIResponse<object> { Success = true, Message = "OK", Data = health });
        }

        [HttpGet("backup/modules")]
        public async Task<IActionResult> GetBackupModules()
        {
            var modules = await systemService.GetBackupModulesAsync();
            return Ok(new APIResponse<object> { Success = true, Message = "OK", Data = modules });
        }

        [HttpGet("backup/{module}")]
        public async Task<IActionResult> Backup(string module)
        {
            var (bytes, key) = await systemService.CreateModuleBackupAsync(module);
            var fileName = $"povis-{key}-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
            return File(bytes, "application/json", fileName);
        }

        [HttpPost("restore/{module}")]
        public async Task<IActionResult> Restore(string module, IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No backup file was uploaded.");

            await using var stream = file.OpenReadStream();
            var restored = await systemService.RestoreModuleAsync(module, stream);

            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = $"Restored {restored} record(s) from backup. Restored record timestamps reflect the restore time.",
                Data = new { restored }
            });
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs([FromQuery] TechnicalLogFilterDTO filter)
        {
            var logs = await systemService.GetLogsAsync(filter);
            return Ok(new APIResponse<object> { Success = true, Message = "Technical logs retrieved successfully", Data = logs });
        }
    }
}
