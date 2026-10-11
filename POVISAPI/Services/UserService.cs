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

        var deputationNumber = await ResolveDeputationNumberForCreateAsync(dto.Role, dto.DeputationNumber);

        var user = new User
        {
            Username = dto.Username,
            DeputationNumber = deputationNumber,
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

        user.DeputationNumber = await ResolveDeputationNumberForUpdateAsync(user, dto);
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

    private async Task<string?> ResolveDeputationNumberForCreateAsync(Role role, string? number)
    {
        if (role != Role.ApprehendingOfficer) return null;
        return await ValidateDeputationNumberAsync(number, null);
    }

    private async Task<string?> ResolveDeputationNumberForUpdateAsync(User user, UpdateUserDTO dto)
    {
        if (dto.Role != Role.ApprehendingOfficer) return null;
        if (string.IsNullOrWhiteSpace(dto.DeputationNumber))
        {
            // Keep the existing number (e.g. when only toggling active) unless the user is newly becoming an officer.
            if (user.Role == Role.ApprehendingOfficer && !string.IsNullOrWhiteSpace(user.DeputationNumber))
                return user.DeputationNumber;
            throw new ArgumentException("Deputation number is required for an Apprehending Officer.");
        }
        return await ValidateDeputationNumberAsync(dto.DeputationNumber, user.Id);
    }

    private async Task<string> ValidateDeputationNumberAsync(string? number, Guid? excludeUserId)
    {
        number = number?.Trim();
        if (string.IsNullOrEmpty(number))
            throw new ArgumentException("Deputation number is required for an Apprehending Officer.");
        if (number.Length > 50)
            throw new ArgumentException("Deputation number cannot exceed 50 characters.");

        var taken = await context.Users.AnyAsync(u => u.DeputationNumber == number && u.Id != excludeUserId);
        if (taken)
            throw new ArgumentException("Deputation number is already assigned to another officer.");
        return number;
    }

    private static UserDTO ToDto(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        Role = u.Role,
        IsActive = u.IsActive,
        DeputationNumber = u.DeputationNumber,
        CreatedAt = u.CreatedAt
    };
}
