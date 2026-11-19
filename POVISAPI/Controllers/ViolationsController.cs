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

        // The JWT's "sub" (username) shares a claim type with the user id, so pick the claim that is a Guid.
        private Guid? CurrentUserId() => User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier || c.Type == "nameid")
            .Select(c => Guid.TryParse(c.Value, out var g) ? g : (Guid?)null)
            .FirstOrDefault(g => g.HasValue);

        [HttpGet]
        public async Task<IActionResult> GetAllViolations([FromQuery] ViolationFilterDTO filter)
        {
            // "Recorded by me" filter: scope to the logged-in user's own entries.
            if (filter.Mine && User.IsInRole("ApprehendingOfficer"))
                filter.RecordedByUserId = CurrentUserId() ?? Guid.Empty;

            var violations = await violationService.GetAllAsync(filter);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Violations retrieved successfully",
                Data = violations
            });
        }

        [HttpGet("officers")]
        public async Task<IActionResult> GetOfficers()
        {
            var officers = await violationService.GetOfficersAsync();
            return Ok(new APIResponse<object> { Success = true, Message = "Officers retrieved successfully", Data = officers });
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
            dto.RecordedByUserId = CurrentUserId();
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

        /// <summary>OVR Supervisor review/approval — separate from case status.</summary>
        [Authorize(Roles = "MISPersonnel,OVRSupervisor")]
        [HttpPut("{id}/review")]
        public async Task<IActionResult> SetReviewed(string id, [FromBody] SetReviewedDTO dto)
        {
            var username = User.FindFirst("sub")?.Value;
            var response = await violationService.SetReviewedAsync(id, dto.Reviewed, username);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = dto.Reviewed ? "Violation marked as reviewed" : "Violation review cleared",
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
