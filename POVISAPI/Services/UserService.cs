using Microsoft.EntityFrameworkCore;
using POVISAPI.Common;
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

        var profile = ValidateProfile(dto.FullName, dto.ContactNumber, dto.Email, dto.Address);
        if (profile.FullName == null)
            throw new ArgumentException("Full name is required.");

        var temporaryPassword = PasswordGenerator.GenerateTemporary();

        var user = new User
        {
            Username = dto.Username,
            FullName = profile.FullName,
            ContactNumber = profile.ContactNumber,
            Email = profile.Email,
            Address = profile.Address,
            DeputationNumber = deputationNumber,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
            MustChangePassword = true,
            Role = dto.Role,
            IsActive = true
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var created = ToDto(user);
        created.TemporaryPassword = temporaryPassword;
        return created;
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
        var profile = ValidateProfile(dto.FullName, dto.ContactNumber, dto.Email, dto.Address);
        // Existing accounts created before these fields existed may have no name; never blank out a name that is set.
        user.FullName = profile.FullName ?? user.FullName;
        user.ContactNumber = profile.ContactNumber;
        user.Email = profile.Email;
        user.Address = profile.Address;

        user.Role = dto.Role;
        user.IsActive = dto.IsActive;

        await context.SaveChangesAsync();

        return ToDto(user);
    }

    public async Task<string> ResetPasswordAsync(Guid id)
    {
        var user = await context.Users.FindAsync(id)
            ?? throw new KeyNotFoundException("User does not exist.");

        var temporaryPassword = PasswordGenerator.GenerateTemporary();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
        user.MustChangePassword = true;
        await context.SaveChangesAsync();
        return temporaryPassword;
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

    private static string? Clean(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static (string? FullName, string? ContactNumber, string? Email, string? Address) ValidateProfile(
        string? fullName, string? contactNumber, string? email, string? address)
    {
        var profile = (FullName: Clean(fullName), ContactNumber: Clean(contactNumber), Email: Clean(email), Address: Clean(address));

        if (profile.FullName is { Length: > 150 })
            throw new ArgumentException("Full name cannot exceed 150 characters.");
        if (profile.ContactNumber is { Length: > 30 })
            throw new ArgumentException("Contact number cannot exceed 30 characters.");
        if (profile.Address is { Length: > 300 })
            throw new ArgumentException("Address cannot exceed 300 characters.");
        if (profile.Email != null)
        {
            if (profile.Email.Length > 150)
                throw new ArgumentException("Email address cannot exceed 150 characters.");
            bool valid;
            try { valid = new System.Net.Mail.MailAddress(profile.Email).Address == profile.Email; }
            catch (FormatException) { valid = false; }
            if (!valid)
                throw new ArgumentException("Email address is not valid.");
        }
        return profile;
    }

    private static UserDTO ToDto(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        Role = u.Role,
        IsActive = u.IsActive,
        DeputationNumber = u.DeputationNumber,
        FullName = u.FullName,
        ContactNumber = u.ContactNumber,
        Email = u.Email,
        Address = u.Address,
        MustChangePassword = u.MustChangePassword,
        CreatedAt = u.CreatedAt
    };
}
