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
    public class CommunityServiceRecordsController : Controller
    {
        private readonly ICommunityServiceRecordService communityServiceRecordService;
        public CommunityServiceRecordsController(ICommunityServiceRecordService communityServiceRecordService)
        {
            this.communityServiceRecordService = communityServiceRecordService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCommunityServiceRecords([FromQuery] ComplianceStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var records = await communityServiceRecordService.GetAllAsync(status, page, pageSize);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Community service records retrieved successfully",
                Data = records
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCommunityServiceRecordById(string id)
        {
            var record = await communityServiceRecordService.GetByIdAsync(id);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Community service record retrieved successfully",
                Data = record
            });
        }

        [HttpPost]
        public async Task<IActionResult> AddCommunityServiceRecord(AddCommunityServiceDTO dto)
        {
            var response = await communityServiceRecordService.CreateAsync(dto);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Community service record created successfully",
                Data = response
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCommunityServiceRecord(string id, [FromBody] UpdateCommunityServiceDTO dto)
        {
            if (id != dto.CommunityServiceRecordId)
                return BadRequest(new APIResponse<object>
                {
                    Success = false,
                    Message = "ID mismatch between route and body."
                });

            var response = await communityServiceRecordService.UpdateAsync(dto);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Community service record updated successfully",
                Data = response
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCommunityServiceRecord(string id)
        {
            await communityServiceRecordService.DeleteAsync(id);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Community service record deleted successfully",
            });
        }
    }
}
