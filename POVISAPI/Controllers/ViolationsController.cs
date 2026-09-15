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
    public class ViolationsController : Controller
    {
        private readonly IViolationService violationService;
        public ViolationsController(IViolationService violationService)
        {
            this.violationService = violationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllViolations([FromQuery] ViolationFilterDTO filter)
        {
            var violations = await violationService.GetAllAsync(filter);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Violations retrieved successfully",
                Data = violations
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetViolationById(string id)
        {
            var violation = await violationService.GetByIdAsync(id);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Violation retrieved successfully",
                Data = violation
            });
        }

        /// <summary>Resolves the applicable fine/penalty from the ordinance schedule.</summary>
        [HttpGet("{id}/penalty")]
        public async Task<IActionResult> GetApplicablePenalty(string id)
        {
            var penalty = await violationService.GetApplicablePenaltyAsync(id);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Applicable penalty retrieved successfully",
                Data = penalty
            });
        }

        [Authorize(Roles = "MISPersonnel,OVRSupervisor,ApprehendingOfficer")]
        [HttpPost]
        public async Task<IActionResult> AddViolation(AddViolationDTO dto)
        {
            var response = await violationService.CreateAsync(dto);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Violation recorded successfully",
                Data = response
            });
        }

        [Authorize(Roles = "MISPersonnel,OVRSupervisor")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateViolation(string id, [FromBody] UpdateViolationDTO dto)
        {
            if (id != dto.ViolationId)
                return BadRequest(new APIResponse<object>
                {
                    Success = false,
                    Message = "ID mismatch between route and body."
                });

            var response = await violationService.UpdateAsync(dto);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Violation updated successfully",
                Data = response
            });
        }

        [Authorize(Roles = "MISPersonnel")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteViolation(string id)
        {
            await violationService.DeleteAsync(id);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Violation deleted successfully",
            });
        }
    }
}
