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
    private readonly ISmsService smsService;
    private readonly ILogger<UserService> logger;

    public UserService(AppDbContext context, ISmsService smsService, ILogger<UserService> logger)
    {
        this.context = context;
        this.smsService = smsService;
        this.logger = logger;
    }

    public async Task<List<UserDTO>> GetAllAsync()
    {
        var users = await context.Users
            .Include(u => u.Barangay)
            .OrderBy(u => u.Username)
            .ToListAsync();

        return users.Select(ToDto).ToList();
    }

    public async Task<UserDTO> GetByIdAsync(Guid id)
    {
        var user = await context.Users.Include(u => u.Barangay).FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new KeyNotFoundException("User does not exist.");

        return ToDto(user);
    }

    private async Task<Barangay?> ResolveBarangayAsync(Guid? barangayId)
    {
        if (barangayId == null) return null;
        return await context.Barangays.FirstOrDefaultAsync(b => b.Id == barangayId)
            ?? throw new ArgumentException("Selected barangay does not exist.");
    }

    public async Task<UserDTO> CreateAsync(CreateUserDTO dto)
    {
        var usernameTaken = await context.Users.AnyAsync(u => u.Username == dto.Username);
        if (usernameTaken)
            throw new ArgumentException("Username is already taken.");

        var deputationNumber = await ResolveDeputationNumberForCreateAsync(dto.Role, dto.DeputationNumber);
        if (dto.Role == Role.ApprehendingOfficer && dto.Division == null)
            throw new ArgumentException("Department is required for an Apprehending Officer.");

        var profile = ValidateProfile(dto.FullName, dto.ContactNumber, dto.Email, dto.Address);
        if (profile.FullName == null)
            throw new ArgumentException("Full name is required.");

        var temporaryPassword = PasswordGenerator.GenerateTemporary();
        var barangay = await ResolveBarangayAsync(dto.BarangayId);

        var user = new User
        {
            BarangayId = barangay?.Id,
            Barangay = barangay,
            Username = dto.Username,
            FullName = profile.FullName,
            ContactNumber = profile.ContactNumber,
            Email = profile.Email,
            Address = profile.Address,
            MaritalStatus = dto.MaritalStatus,
            Photo = ValidatePhoto(dto.Photo),
            DeputationNumber = deputationNumber,
            Division = dto.Role == Role.ApprehendingOfficer ? dto.Division : null,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
            MustChangePassword = true,
            Role = dto.Role,
            IsActive = true
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var created = ToDto(user);
        created.TemporaryPassword = temporaryPassword;

        // Text the credentials to the registered number. The account already exists, so an SMS problem must not
        // fail the request: MIS Personnel still gets the temporary password on screen and is told the text did not go out.
        var number = SmsService.NormalizeRecipient(user.ContactNumber);
        if (number is null)
        {
            created.SmsSent = false;
            created.SmsNote = user.ContactNumber is null
                ? "No cellphone number on the account, so no text was sent."
                : "The cellphone number is not a valid Philippine mobile number, so no text was sent.";
        }
        else
        {
            try
            {
                await smsService.SendAsync(number,
                    $"POVIS: Your account was created. Username: {user.Username}. Temporary password: {temporaryPassword}. You will be asked to set a new password when you sign in. Do not share this with anyone.");
                created.SmsSent = true;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                logger.LogWarning("Account {Username} created but the temporary password SMS failed: {Reason}", user.Username, ex.Message);
                created.SmsSent = false;
                created.SmsNote = ex.Message;
            }
        }
        return created;
    }

    public async Task<UserDTO> UpdateAsync(UpdateUserDTO dto)
    {
        var user = await context.Users.Include(u => u.Barangay).FirstOrDefaultAsync(u => u.Id == dto.Id)
            ?? throw new KeyNotFoundException("User does not exist.");

        var newUsername = dto.Username?.Trim();
        if (!string.IsNullOrEmpty(newUsername) && newUsername != user.Username)
        {
            if (newUsername.Length > 100)
                throw new ArgumentException("Username cannot exceed 100 characters.");
            var taken = await context.Users.AnyAsync(u => u.Username == newUsername && u.Id != user.Id);
            if (taken)
                throw new ArgumentException("Username is already taken.");
            user.Username = newUsername;
        }

        var barangay = await ResolveBarangayAsync(dto.BarangayId);
        user.Barangay = barangay;
        user.BarangayId = barangay?.Id;

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
        // Keep the stored department when none is sent (e.g. when only toggling active); only officers have one.
        user.Division = dto.Role == Role.ApprehendingOfficer ? dto.Division ?? user.Division : null;
        var profile = ValidateProfile(dto.FullName, dto.ContactNumber, dto.Email, dto.Address);
        // Existing accounts created before these fields existed may have no name; never blank out a name that is set.
        user.FullName = profile.FullName ?? user.FullName;
        user.ContactNumber = profile.ContactNumber;
        user.Email = profile.Email;
        user.Address = profile.Address;
        user.MaritalStatus = dto.MaritalStatus;
        if (dto.Photo != null)
            user.Photo = ValidatePhoto(dto.Photo); // "" clears it

        user.Role = dto.Role;
        user.IsActive = dto.IsActive;

        await context.SaveChangesAsync();

        return ToDto(user);
    }

    public async Task<UserDTO> UpdateProfileAsync(Guid id, UpdateProfileDTO dto)
    {
        var user = await context.Users.Include(u => u.Barangay).FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new KeyNotFoundException("User does not exist.");

        var profile = ValidateProfile(dto.FullName, dto.ContactNumber, dto.Email, dto.Address);
        if (profile.FullName == null)
            throw new ArgumentException("Full name is required.");

        user.FullName = profile.FullName;
        user.ContactNumber = profile.ContactNumber;
        user.Email = profile.Email;
        user.Address = profile.Address;
        user.MaritalStatus = dto.MaritalStatus;
        if (dto.Photo != null)
            user.Photo = ValidatePhoto(dto.Photo); // "" clears it

        await context.SaveChangesAsync();
        return ToDto(user);
    }

    public async Task<string> ResetPasswordAsync(Guid id)
    {
        var user = await context.Users.FindAsync(id)
            ?? throw new KeyNotFoundException("User does not exist.");

        var temporaryPassword = PasswordGenerator.GenerateTemporaryFor(user);
        PasswordGenerator.ApplyTemporary(user, temporaryPassword);
        await context.SaveChangesAsync();
        return temporaryPassword;
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

    private const int MaxPhotoBytes = 1_000_000;

    private static string? ValidatePhoto(string? photo)
    {
        if (string.IsNullOrWhiteSpace(photo)) return null;
        return ImageDataValidator.Validate(photo, MaxPhotoBytes, "Photo").DataUrl;
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
        BarangayId = u.BarangayId,
        BarangayName = u.Barangay?.Name,
        Division = u.Division,
        FullName = u.FullName,
        ContactNumber = u.ContactNumber,
        Email = u.Email,
        Address = u.Address,
        MaritalStatus = u.MaritalStatus,
        Photo = u.Photo,
        MustChangePassword = u.MustChangePassword,
        CreatedAt = u.CreatedAt
    };
}
