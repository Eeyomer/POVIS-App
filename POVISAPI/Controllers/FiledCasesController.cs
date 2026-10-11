using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using POVISAPI.Interfaces;
using POVISAPI.Models.DTO;
using POVISAPI.Models.Entity.Enums;
using POVISAPI.Models.System_Models;

namespace POVISAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowFrontend")]
    [Authorize]
    public class FiledCasesController : Controller
    {
        private readonly IFiledCaseService filedCaseService;
        public FiledCasesController(IFiledCaseService filedCaseService)
        {
            this.filedCaseService = filedCaseService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFiledCases([FromQuery] CaseStatus? status, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        {
            var filedCases = await filedCaseService.GetAllAsync(status, from, to, page, pageSize, search);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Filed cases retrieved successfully",
                Data = filedCases
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFiledCaseById(string id)
        {
            var filedCase = await filedCaseService.GetByIdAsync(id);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Filed case retrieved successfully",
                Data = filedCase
            });
        }

        [Authorize(Roles = "MISPersonnel,OVRSupervisor")]
        [HttpPost]
        public async Task<IActionResult> AddFiledCase(AddFiledCaseDTO dto)
        {
            var response = await filedCaseService.CreateAsync(dto);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Case filed successfully",
                Data = response
            });
        }

        [Authorize(Roles = "MISPersonnel,OVRSupervisor")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFiledCase(string id, [FromBody] UpdateFiledCaseDTO dto)
        {
            if (id != dto.FiledCaseId)
                return BadRequest(new APIResponse<object>
                {
                    Success = false,
                    Message = "ID mismatch between route and body."
                });

            var response = await filedCaseService.UpdateAsync(dto);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Filed case updated successfully",
                Data = response
            });
        }

        [Authorize(Roles = "MISPersonnel")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFiledCase(string id)
        {
            await filedCaseService.DeleteAsync(id);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Filed case deleted successfully",
            });
        }
    }
}
