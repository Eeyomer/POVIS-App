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
    public class AuthController : Controller
    {
        private readonly IAuthService authService;
        public AuthController(IAuthService authService)
        {
            this.authService = authService;
        }

        [AllowAnonymous]
        [HttpGet("admin-exists")]
        public async Task<IActionResult> AdminExists()
        {
            var exists = await authService.AdminExistsAsync();
            return Ok(new APIResponse<object> { Success = true, Message = "OK", Data = new { exists } });
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDTO dto)
        {
            var response = await authService.RegisterAsync(dto);
            return Ok(new APIResponse<object> { Success = true, Message = "Account created successfully", Data = response });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDTO dto)
        {
            var response = await authService.LoginAsync(dto);
            return Ok(new APIResponse<object> { Success = true, Message = "Login successful", Data = response });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDTO dto)
        {
            var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(idClaim, out var userId))
                return Unauthorized();

            var response = await authService.ChangePasswordAsync(userId, dto);
            return Ok(new APIResponse<object> { Success = true, Message = "Password changed successfully", Data = response });
        }
    }
}
