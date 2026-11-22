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
        private readonly IUserService userService;
        public AuthController(IAuthService authService, IUserService userService)
        {
            this.authService = authService;
            this.userService = userService;
        }

        private Guid? CurrentUserId() => User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier || c.Type == "nameid")
            .Select(c => Guid.TryParse(c.Value, out var g) ? g : (Guid?)null)
            .FirstOrDefault(g => g.HasValue);

        [Authorize]
        [HttpGet("me/details")]
        public async Task<IActionResult> GetMyDetails()
        {
            var userId = CurrentUserId();
            if (userId is null) return Unauthorized();
            var user = await userService.GetByIdAsync(userId.Value);
            return Ok(new APIResponse<object> { Success = true, Message = "OK", Data = user });
        }

        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe(UpdateProfileDTO dto)
        {
            var userId = CurrentUserId();
            if (userId is null) return Unauthorized();
            var user = await userService.UpdateProfileAsync(userId.Value, dto);
            return Ok(new APIResponse<object> { Success = true, Message = "Profile updated successfully", Data = user });
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

        /// <summary>Forgot password: needs the username AND the cellphone number registered on the account; texts a temporary password.</summary>
        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO dto)
        {
            var result = await authService.ForgotPasswordAsync(dto, HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(new APIResponse<ForgotPasswordResultDTO>
            {
                Success = true,
                Message = "A temporary password has been sent to your registered cellphone number.",
                Data = result
            });
        }

        /// <summary>The signed-in user's name and photo, for the sidebar.</summary>
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            // The JWT's "sub" (username) shares a claim type with the user id, so pick the claim that is a Guid.
            var userId = User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier || c.Type == "nameid")
                .Select(c => Guid.TryParse(c.Value, out var g) ? g : (Guid?)null)
                .FirstOrDefault(g => g.HasValue);
            if (userId is null)
                return Unauthorized();

            var profile = await authService.GetProfileAsync(userId.Value);
            return Ok(new APIResponse<object> { Success = true, Message = "Profile retrieved successfully", Data = profile });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDTO dto)
        {
            // The JWT's "sub" (username) is mapped to the same claim type as the user id, so FindFirst may
            // return the username. Pick the first claim of those types that is actually a Guid.
            var userId = User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier || c.Type == "nameid")
                .Select(c => Guid.TryParse(c.Value, out var g) ? g : (Guid?)null)
                .FirstOrDefault(g => g.HasValue);
            if (userId is null)
                return Unauthorized();

            var response = await authService.ChangePasswordAsync(userId.Value, dto);
            return Ok(new APIResponse<object> { Success = true, Message = "Password changed successfully", Data = response });
        }
    }
}
