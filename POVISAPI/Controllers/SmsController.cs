using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using POVISAPI.Interfaces;
using POVISAPI.Models.System_Models;
using POVISAPI.Services;

namespace POVISAPI.Controllers
{
    public class SendSmsDTO
    {
        [Required(ErrorMessage = "Recipient is required")]
        public string Recipient { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required")]
        [StringLength(SmsService.MaxMessageLength, ErrorMessage = "Message cannot exceed 480 characters")]
        public string Message { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowFrontend")]
    [Authorize(Roles = "MISPersonnel,OVRSupervisor")]
    public class SmsController : Controller
    {
        private readonly ISmsService smsService;
        public SmsController(ISmsService smsService) => this.smsService = smsService;

        /// <summary>Whether SMS is configured (never returns the token or sender id).</summary>
        [HttpGet("status")]
        public IActionResult Status() =>
            Ok(new APIResponse<object> { Success = true, Message = "SMS status retrieved", Data = new { configured = smsService.IsConfigured } });

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] SendSmsDTO dto, CancellationToken ct)
        {
            await smsService.SendAsync(dto.Recipient, dto.Message, ct);
            return Ok(new APIResponse<object> { Success = true, Message = "SMS sent successfully" });
        }
    }
}
