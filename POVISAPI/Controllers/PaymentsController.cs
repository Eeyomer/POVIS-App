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
    public class PaymentsController : Controller
    {
        private readonly IPaymentService paymentService;
        public PaymentsController(IPaymentService paymentService)
        {
            this.paymentService = paymentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPayments([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var payments = await paymentService.GetAllAsync(from, to, page, pageSize);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Payments retrieved successfully",
                Data = payments
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPaymentById(string id)
        {
            var payment = await paymentService.GetByIdAsync(id);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Payment retrieved successfully",
                Data = payment
            });
        }

        [HttpGet("by-violation/{violationId}")]
        public async Task<IActionResult> GetByViolation(string violationId)
        {
            var payment = await paymentService.GetByViolationIdAsync(violationId);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Payment retrieved successfully",
                Data = payment
            });
        }

        [Authorize(Roles = "MISPersonnel,OVRSupervisor")]
        [HttpPost]
        public async Task<IActionResult> AddPayment(AddPaymentDTO dto)
        {
            var response = await paymentService.CreateAsync(dto);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Payment recorded successfully",
                Data = response
            });
        }

        [Authorize(Roles = "MISPersonnel,OVRSupervisor")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePayment(string id, [FromBody] UpdatePaymentDTO dto)
        {
            if (id != dto.PaymentId)
                return BadRequest(new APIResponse<object>
                {
                    Success = false,
                    Message = "ID mismatch between route and body."
                });

            var response = await paymentService.UpdateAsync(dto);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Payment updated successfully",
                Data = response
            });
        }

        [Authorize(Roles = "MISPersonnel")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePayment(string id)
        {
            await paymentService.DeleteAsync(id);
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "Payment voided successfully",
            });
        }
    }
}
