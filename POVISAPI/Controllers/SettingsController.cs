using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using POVISAPI.Interfaces;
using POVISAPI.Models.DTO;
using POVISAPI.Models.System_Models;

namespace POVISAPI.Controllers
{
    /// <summary>System-wide settings: readable by every signed-in user, changed by MIS Personnel only.</summary>
    [ApiController]
    [Route("api/settings")]
    [EnableCors("AllowFrontend")]
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly IAppSettingsService settingsService;
        public SettingsController(IAppSettingsService settingsService)
        {
            this.settingsService = settingsService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var settings = await settingsService.GetAsync();
            return Ok(new APIResponse<AppSettingsDTO> { Success = true, Message = "Settings retrieved successfully", Data = settings });
        }

        [Authorize(Roles = "MISPersonnel")]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] AppSettingsDTO settings)
        {
            var updated = await settingsService.UpdateAsync(settings);
            return Ok(new APIResponse<AppSettingsDTO> { Success = true, Message = "Settings updated successfully", Data = updated });
        }
    }
}
