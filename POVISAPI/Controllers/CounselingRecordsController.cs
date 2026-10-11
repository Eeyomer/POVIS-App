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
    public class CounselingRecordsController : Controller
    {
        private readonly ICounselingRecordService counselingRecordService;
        public CounselingRecordsController(ICounselingRecordService counselingRecordService)
        {
            this.counselingRecordService = counselingRecordService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCounselingRecords([FromQuery] ComplianceStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        {
            var records = await counselingRecordService.GetAllAsync(status, page, pageSize, search);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Counseling records retrieved successfully",
                Data = records
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCounselingRecordById(string id)
        {
            var record = await counselingRecordService.GetByIdAsync(id);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Counseling record retrieved successfully",
                Data = record
            });
        }

        [Authorize(Roles = "MISPersonnel,OVRSupervisor")]
        [HttpPost]
        public async Task<IActionResult> AddCounselingRecord(AddCounselingDTO dto)
        {
            var response = await counselingRecordService.CreateAsync(dto);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Counseling record created successfully",
                Data = response
            });
        }

        [Authorize(Roles = "MISPersonnel,OVRSupervisor")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCounselingRecord(string id, [FromBody] UpdateCounselingDTO dto)
        {
            if (id != dto.CounselingRecordId)
                return BadRequest(new APIResponse<object>
                {
                    Success = false,
                    Message = "ID mismatch between route and body."
                });

            var response = await counselingRecordService.UpdateAsync(dto);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Counseling record updated successfully",
                Data = response
            });
        }

        [Authorize(Roles = "MISPersonnel")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCounselingRecord(string id)
        {
            await counselingRecordService.DeleteAsync(id);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Counseling record deleted successfully",
            });
        }
    }
}
