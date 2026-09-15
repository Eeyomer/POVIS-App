using Microsoft.EntityFrameworkCore;
using POVISAPI.Data;
using POVISAPI.Interfaces;
using POVISAPI.Models.DTO;
using POVISAPI.Models.Entity;
using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Services;

public class UserService : IUserService
{
    private readonly AppDbContext context;

    public UserService(AppDbContext context)
    {
        this.context = context;
    }

    public async Task<List<UserDTO>> GetAllAsync()
    {
        var users = await context.Users
            .OrderBy(u => u.Username)
            .ToListAsync();

        return users.Select(ToDto).ToList();
    }

    public async Task<UserDTO> GetByIdAsync(Guid id)
    {
        var user = await context.Users.FindAsync(id)
            ?? throw new KeyNotFoundException("User does not exist.");

        return ToDto(user);
    }

    public async Task<UserDTO> CreateAsync(CreateUserDTO dto)
    {
        var usernameTaken = await context.Users.AnyAsync(u => u.Username == dto.Username);
        if (usernameTaken)
            throw new ArgumentException("Username is already taken.");

        var user = new User
        {
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            IsActive = true
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return ToDto(user);
    }

    public async Task<UserDTO> UpdateAsync(UpdateUserDTO dto)
    {
        var user = await context.Users.FindAsync(dto.Id)
            ?? throw new KeyNotFoundException("User does not exist.");

        if (user.Role == Role.MISPersonnel && dto.Role != Role.MISPersonnel)
        {
            var otherActiveMis = await context.Users
                .AnyAsync(u => u.Id != dto.Id && u.Role == Role.MISPersonnel && u.IsActive);
            if (!otherActiveMis)
                throw new InvalidOperationException("At least one active MIS Personnel account must remain.");
        }

        if (user.Role == Role.MISPersonnel && !dto.IsActive)
        {
            var otherActiveMis = await context.Users
                .AnyAsync(u => u.Id != dto.Id && u.Role == Role.MISPersonnel && u.IsActive);
            if (!otherActiveMis)
                throw new InvalidOperationException("At least one active MIS Personnel account must remain.");
        }

        user.Role = dto.Role;
        user.IsActive = dto.IsActive;

        await context.SaveChangesAsync();

        return ToDto(user);
    }

    public async Task ResetPasswordAsync(Guid id, ResetPasswordDTO dto)
    {
        var user = await context.Users.FindAsync(id)
            ?? throw new KeyNotFoundException("User does not exist.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await context.Users.FindAsync(id)
            ?? throw new KeyNotFoundException("User does not exist.");

        if (user.Role == Role.MISPersonnel)
        {
            var otherActiveMis = await context.Users
                .AnyAsync(u => u.Id != id && u.Role == Role.MISPersonnel && u.IsActive);
            if (!otherActiveMis)
                throw new InvalidOperationException("At least one active MIS Personnel account must remain.");
        }

        context.Users.Remove(user);
        await context.SaveChangesAsync();
    }

    private static UserDTO ToDto(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        Role = u.Role,
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt
    };
}
