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
    public class BarangaysController : Controller
    {
        private readonly IBarangayService barangayService;
        public BarangaysController(IBarangayService barangayService)
        {
            this.barangayService = barangayService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBarangays([FromQuery] bool includeInactive = false)
        {
            var barangays = await barangayService.GetAllAsync(includeInactive);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Barangays retrieved successfully",
                Data = barangays
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBarangayById(string id)
        {
            var barangay = await barangayService.GetByIdAsync(id);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Barangay retrieved successfully",
                Data = barangay
            });
        }

        [Authorize(Roles = "MISPersonnel")]
        [HttpPost]
        public async Task<IActionResult> AddBarangay(AddBarangayDTO dto)
        {
            var response = await barangayService.CreateAsync(dto);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Barangay created successfully",
                Data = response
            });
        }

        [Authorize(Roles = "MISPersonnel")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBarangay(string id, [FromBody] UpdateBarangayDTO dto)
        {
            if (id != dto.BarangayId)
                return BadRequest(new APIResponse<object>
                {
                    Success = false,
                    Message = "ID mismatch between route and body."
                });

            var response = await barangayService.UpdateAsync(dto);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Barangay updated successfully",
                Data = response
            });
        }

        [Authorize(Roles = "MISPersonnel")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBarangay(string id)
        {
            await barangayService.DeleteAsync(id);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Barangay deleted successfully",
            });
        }
    }
}
