using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using POVISAPI.Data;
using POVISAPI.Middleware;
using POVISAPI.Interfaces;
using POVISAPI.Models.DTO;
using POVISAPI.Models.Entity;
using POVISAPI.Models.Entity.Enums;
using POVISAPI.Models.System_Models;

namespace POVISAPI.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext context;
    private readonly JwtSettings jwtSettings;

    public AuthService(AppDbContext context, IOptions<JwtSettings> jwtSettings)
    {
        this.context = context;
        this.jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponseDTO> RegisterAsync(RegisterDTO dto)
    {
        // Public self-registration is only for bootstrapping the very first MIS Personnel
        // account. Once one exists, additional accounts must be created via the (MIS
        // Personnel-only) Users management endpoints.
        if (await AdminExistsAsync())
            throw new InvalidOperationException("An MIS Personnel account already exists. Ask an MIS Personnel to create your account.");

        var usernameTaken = await context.Users.AnyAsync(u => u.Username == dto.Username);
        if (usernameTaken)
            throw new ArgumentException("Username is already taken.");

        var user = new User
        {
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = Role.MISPersonnel,
            IsActive = true
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponseDTO> LoginAsync(LoginDTO dto)
    {
        var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == dto.Username);

        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new AuthenticationException("Invalid username or password.");

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponseDTO> ChangePasswordAsync(Guid userId, ChangePasswordDTO dto)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive)
            ?? throw new KeyNotFoundException("User does not exist.");

        // A user forced to change a temporary password is already authenticated with it, so they are not asked again.
        if (!user.MustChangePassword && !BCrypt.Net.BCrypt.Verify(dto.CurrentPassword ?? string.Empty, user.PasswordHash))
            throw new ArgumentException("Current password is incorrect.");
        if (string.IsNullOrEmpty(dto.NewPassword) || dto.NewPassword.Length < 8)
            throw new ArgumentException("New password must be at least 8 characters.");
        if (dto.NewPassword.Length > 100)
            throw new ArgumentException("New password cannot exceed 100 characters.");
        if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash))
            throw new ArgumentException("New password must be different from the current password.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.MustChangePassword = false;
        await context.SaveChangesAsync();

        return BuildAuthResponse(user);
    }

    public async Task<bool> AdminExistsAsync()
    {
        return await context.Users.AnyAsync(u => u.Role == Role.MISPersonnel && u.IsActive);
    }

    private AuthResponseDTO BuildAuthResponse(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.ExpiryInMinutes);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        if (user.MustChangePassword)
            claims.Add(new Claim(PasswordChangeRequiredMiddleware.ClaimType, "true"));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return new AuthResponseDTO
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Username = user.Username,
            Role = user.Role,
            ExpiresAt = expiresAt,
            MustChangePassword = user.MustChangePassword
        };
    }
}
