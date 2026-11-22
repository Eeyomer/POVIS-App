using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using POVISAPI.Interfaces;
using POVISAPI.Models.DTO;
using POVISAPI.Models.System_Models;

namespace POVISAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowFrontend")]
    [Authorize]
    public class IncentivesController : Controller
    {
        private readonly IIncentiveService incentiveService;
        public IncentivesController(IIncentiveService incentiveService) => this.incentiveService = incentiveService;

        // The JWT's "sub" (username) shares a claim type with the user id, so pick the claim that is a Guid.
        private Guid? CurrentUserId() => User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier || c.Type == "nameid")
            .Select(c => Guid.TryParse(c.Value, out var g) ? g : (Guid?)null)
            .FirstOrDefault(g => g.HasValue);

        /// <summary>All officers' incentives (MIS Personnel / OVR Supervisor), filterable by officer, division, period.</summary>
        [Authorize(Roles = "MISPersonnel,OVRSupervisor")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] IncentiveFilterDTO filter)
        {
            var data = await incentiveService.GetAsync(filter, null);
            return Ok(new APIResponse<IncentiveListDTO> { Success = true, Message = "Incentives retrieved successfully", Data = data });
        }

        /// <summary>The signed-in Apprehending Officer's own incentives.</summary>
        [Authorize(Roles = "ApprehendingOfficer")]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine([FromQuery] IncentiveFilterDTO filter)
        {
            var me = CurrentUserId() ?? Guid.Empty;
            var data = await incentiveService.GetAsync(filter, me);
            return Ok(new APIResponse<IncentiveListDTO> { Success = true, Message = "Incentives retrieved successfully", Data = data });
        }
    }
}
