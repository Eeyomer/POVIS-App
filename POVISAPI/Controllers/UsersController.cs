using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using POVISAPI.Interfaces;
using POVISAPI.Models.DTO;
using POVISAPI.Models.System_Models;

namespace POVISAPI.Controllers
{
    /// <summary>Account and role management — MIS Personnel responsibility only.</summary>
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowFrontend")]
    [Authorize(Roles = "MISPersonnel")]
    public class UsersController : Controller
    {
        private readonly IUserService userService;
        public UsersController(IUserService userService)
        {
            this.userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await userService.GetAllAsync();
            return Ok(new APIResponse<object> { Success = true, Message = "Users retrieved successfully", Data = users });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var user = await userService.GetByIdAsync(id);
            return Ok(new APIResponse<object> { Success = true, Message = "User retrieved successfully", Data = user });
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserDTO dto)
        {
            var user = await userService.CreateAsync(dto);
            return Ok(new APIResponse<object> { Success = true, Message = "User created successfully", Data = user });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDTO dto)
        {
            if (id != dto.Id)
                return BadRequest(new APIResponse<object> { Success = false, Message = "ID mismatch between route and body." });

            var user = await userService.UpdateAsync(dto);
            return Ok(new APIResponse<object> { Success = true, Message = "User updated successfully", Data = user });
        }

        [HttpPut("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordDTO dto)
        {
            await userService.ResetPasswordAsync(id, dto);
            return Ok(new APIResponse<object> { Success = true, Message = "Password reset successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            await userService.DeleteAsync(id);
            return Ok(new APIResponse<object> { Success = true, Message = "User deleted successfully" });
        }
    }
}
