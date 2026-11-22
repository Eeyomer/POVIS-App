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

        /// <summary>Sets the incentive %. Only payments recorded after this call use it; earlier payments keep the rate they were recorded with.</summary>
        [Authorize(Roles = "MISPersonnel")]
        [HttpPut("incentive-rate")]
        public async Task<IActionResult> UpdateIncentiveRate([FromBody] IncentiveRateDTO dto)
        {
            await settingsService.UpdateIncentiveRateAsync(dto.Rate);
            var settings = await settingsService.GetAsync();
            return Ok(new APIResponse<AppSettingsDTO> { Success = true, Message = "Incentive rate updated successfully", Data = settings });
        }
    }
}
