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
    public class ViolatorsController : Controller
    {
        private readonly IViolatorService violatorService;
        public ViolatorsController(IViolatorService violatorService)
        {
            this.violatorService = violatorService;
        }

        [HttpGet]
        public async Task<IActionResult> SearchViolators([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var violators = await violatorService.SearchAsync(search, page, pageSize);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Violators retrieved successfully",
                Data = violators
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetViolatorById(string id)
        {
            var violator = await violatorService.GetByIdAsync(id);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Violator retrieved successfully",
                Data = violator
            });
        }

        /// <summary>Offense history — used at ticketing to show prior violations.</summary>
        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetViolatorHistory(string id)
        {
            var history = await violatorService.GetHistoryAsync(id);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Violator history retrieved successfully",
                Data = history
            });
        }

        [Authorize(Roles = "MISPersonnel,OVRSupervisor,ApprehendingOfficer")]
        [HttpPost]
        public async Task<IActionResult> AddViolator(AddViolatorDTO dto)
        {
            var response = await violatorService.CreateAsync(dto);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Violator created successfully",
                Data = response
            });
        }

        [Authorize(Roles = "MISPersonnel,OVRSupervisor,ApprehendingOfficer")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateViolator(string id, [FromBody] UpdateViolatorDTO dto)
        {
            if (id != dto.ViolatorId)
                return BadRequest(new APIResponse<object>
                {
                    Success = false,
                    Message = "ID mismatch between route and body."
                });

            var response = await violatorService.UpdateAsync(dto);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Violator updated successfully",
                Data = response
            });
        }

        [Authorize(Roles = "MISPersonnel")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteViolator(string id)
        {
            await violatorService.DeleteAsync(id);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Violator deleted successfully",
            });
        }
    }
}
