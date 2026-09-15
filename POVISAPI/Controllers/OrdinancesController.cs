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
    public class OrdinancesController : Controller
    {
        private readonly IOrdinanceService ordinanceService;
        public OrdinancesController(IOrdinanceService ordinanceService)
        {
            this.ordinanceService = ordinanceService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllOrdinances([FromQuery] bool includeInactive = false)
        {
            var ordinances = await ordinanceService.GetAllAsync(includeInactive);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Ordinances retrieved successfully",
                Data = ordinances
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrdinanceById(string id)
        {
            var ordinance = await ordinanceService.GetByIdAsync(id);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Ordinance retrieved successfully",
                Data = ordinance
            });
        }

        [Authorize(Roles = "MISPersonnel")]
        [HttpPost]
        public async Task<IActionResult> AddOrdinance(AddOrdinanceDTO dto)
        {
            var response = await ordinanceService.CreateAsync(dto);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Ordinance created successfully",
                Data = response
            });
        }

        [Authorize(Roles = "MISPersonnel")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrdinance(string id, [FromBody] UpdateOrdinanceDTO dto)
        {
            if (id != dto.OrdinanceId)
                return BadRequest(new APIResponse<object>
                {
                    Success = false,
                    Message = "ID mismatch between route and body."
                });

            var response = await ordinanceService.UpdateAsync(dto);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Ordinance updated successfully",
                Data = response
            });
        }

        [Authorize(Roles = "MISPersonnel")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrdinance(string id)
        {
            await ordinanceService.DeleteAsync(id);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Ordinance deleted successfully",
            });
        }

        // ----- Sections -----
        [Authorize(Roles = "MISPersonnel")]
        [HttpPost("{id}/sections")]
        public async Task<IActionResult> AddSection(string id, AddOrdinanceSectionDTO dto)
        {
            var response = await ordinanceService.AddSectionAsync(id, dto);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Section added successfully",
                Data = response
            });
        }

        [Authorize(Roles = "MISPersonnel")]
        [HttpDelete("{id}/sections/{sectionId}")]
        public async Task<IActionResult> RemoveSection(string id, string sectionId)
        {
            await ordinanceService.RemoveSectionAsync(id, sectionId);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Section removed successfully",
            });
        }

        // ----- Penalties -----
        [Authorize(Roles = "MISPersonnel")]
        [HttpPost("{id}/penalties")]
        public async Task<IActionResult> AddPenalty(string id, AddOrdinancePenaltyDTO dto)
        {
            var response = await ordinanceService.AddPenaltyAsync(id, dto);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Penalty added successfully",
                Data = response
            });
        }

        [Authorize(Roles = "MISPersonnel")]
        [HttpDelete("{id}/penalties/{penaltyId}")]
        public async Task<IActionResult> RemovePenalty(string id, string penaltyId)
        {
            await ordinanceService.RemovePenaltyAsync(id, penaltyId);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Penalty removed successfully",
            });
        }
    }
}
