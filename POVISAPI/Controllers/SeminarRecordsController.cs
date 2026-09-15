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
    public class SeminarRecordsController : Controller
    {
        private readonly ISeminarRecordService seminarRecordService;
        public SeminarRecordsController(ISeminarRecordService seminarRecordService)
        {
            this.seminarRecordService = seminarRecordService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSeminarRecords([FromQuery] SeminarType? type, [FromQuery] ComplianceStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var records = await seminarRecordService.GetAllAsync(type, status, page, pageSize);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Seminar records retrieved successfully",
                Data = records
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSeminarRecordById(string id)
        {
            var record = await seminarRecordService.GetByIdAsync(id);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Seminar record retrieved successfully",
                Data = record
            });
        }

        [Authorize(Roles = "MISPersonnel,OVRSupervisor")]
        [HttpPost]
        public async Task<IActionResult> AddSeminarRecord(AddSeminarRecordDTO dto)
        {
            var response = await seminarRecordService.CreateAsync(dto);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Seminar record created successfully",
                Data = response
            });
        }

        [Authorize(Roles = "MISPersonnel,OVRSupervisor")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSeminarRecord(string id, [FromBody] UpdateSeminarRecordDTO dto)
        {
            if (id != dto.SeminarRecordId)
                return BadRequest(new APIResponse<object>
                {
                    Success = false,
                    Message = "ID mismatch between route and body."
                });

            var response = await seminarRecordService.UpdateAsync(dto);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Seminar record updated successfully",
                Data = response
            });
        }

        [Authorize(Roles = "MISPersonnel")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSeminarRecord(string id)
        {
            await seminarRecordService.DeleteAsync(id);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Seminar record deleted successfully",
            });
        }
    }
}
