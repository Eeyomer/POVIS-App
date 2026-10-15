using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using POVISAPI.Common;
using POVISAPI.Data;
using POVISAPI.Models.DTO;
using POVISAPI.Models.Entity;
using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Services.BulkImport
{
    internal static class Opt
    {
        public static string[] Of<T>() where T : struct, Enum => Enum.GetNames<T>();
        public static readonly string[] YesNo = { "Yes", "No" };
    }

    // ───────────────────────── Barangays ─────────────────────────
    public sealed class BarangayBulkModule : BulkModule
    {
        public override string Key => "barangays";
        public override string Title => "Barangays";
        public override string SheetName => "Barangays";
        public override int MaxRows => 500;

        public override IReadOnlyList<BulkColumn> Columns { get; } = new[]
        {
            new BulkColumn("Barangay Name *", true, "Required. Up to 100 characters. Must be unique (e.g. Bagong Ilog, Ugong)."),
            new BulkColumn("District", false, "Optional. 1 or 2. Leave blank for special areas such as Pasig MegaMarket.", new[] { "1", "2" }),
            new BulkColumn("Is Active", false, "Yes or No. Default Yes.", Opt.YesNo)
        };

        public override IReadOnlyList<string[]> Examples { get; } = new[]
        {
            new[] { "Bagong Ilog", "1", "Yes" },
            new[] { "Ugong", "1", "Yes" },
            new[] { "Santolan", "2", "Yes" },
            new[] { "Pasig MegaMarket", "", "Yes" }
        };

        public override IReadOnlyList<(string, string)> Notes { get; } = new[]
        {
            ("Existing records", "A barangay whose name already exists (any capitalization) is skipped, not changed."),
            ("Upload order", "Upload Barangays first - Violators and Violations refer to barangays by name.")
        };

        public override async Task StageAsync(AppDbContext db, IReadOnlyList<BulkRow> rows, BulkContext ctx)
        {
            var seen = (await db.Barangays.Select(b => b.Name).ToListAsync())
                .Select(n => n.Trim().ToLowerInvariant()).ToHashSet();

            foreach (var r in rows)
            {
                var name = r.Required("Barangay Name", 100);
                var district = r.Int("District", 1, 2);
                var active = r.Bool("Is Active", true);
                if (r.HasErrors) continue;

                if (!seen.Add(name.ToLowerInvariant())) { ctx.Count("Barangays skipped (already exist)"); continue; }
                db.Barangays.Add(new Barangay { Name = name, District = district, IsActive = active });
                ctx.Count("Barangays added");
            }
        }
    }

    // ───────────────────────── Users ─────────────────────────
    public sealed class UserBulkModule : BulkModule
    {
        public override string Key => "users";
        public override string Title => "Users";
        public override string SheetName => "Users";
        public override int MaxRows => 300; // each account needs a password hash

        public override IReadOnlyList<BulkColumn> Columns { get; } = new[]
        {
            new BulkColumn("Username *", true, "Required. Up to 100 characters, no spaces. Must be unique."),
            new BulkColumn("Full Name *", true, "Required. Up to 150 characters."),
            new BulkColumn("Role *", true, "Required. MISPersonnel, OVRSupervisor, ApprehendingOfficer or PNPBarangayStaff.", Opt.Of<Role>()),
            new BulkColumn("Deputation Number", false, "Required for ApprehendingOfficer, ignored for other roles. Up to 50 characters, unique per officer, e.g. 2025-01-440."),
            new BulkColumn("Contact Number", false, "Optional. Up to 30 characters."),
            new BulkColumn("Email", false, "Optional. Valid email address, up to 150 characters."),
            new BulkColumn("Address", false, "Optional. Up to 300 characters."),
            new BulkColumn("Marital Status", false, "Optional. Single, Married, Widowed, Separated or Divorced.", Opt.Of<MaritalStatus>()),
            new BulkColumn("Is Active", false, "Yes or No. Default Yes.", Opt.YesNo)
        };

        public override IReadOnlyList<string[]> Examples { get; } = new[]
        {
            new[] { "jdelacruz", "Juan Dela Cruz", "ApprehendingOfficer", "2025-01-440", "09171234567", "juan@example.com", "12 Rizal St., Pasig", "Married", "Yes" },
            new[] { "msantos", "Maria Santos", "OVRSupervisor", "", "09181234567", "", "", "Single", "Yes" }
        };

        public override IReadOnlyList<(string, string)> Notes { get; } = new[]
        {
            ("Passwords", "Each new user gets a system-generated temporary password and must change it at first login. The passwords are shown ONCE after a successful upload (with a CSV download) - save them before leaving the page."),
            ("Existing records", "A username that already exists is skipped, not changed."),
            ("Limit", "Users are limited to 300 rows per upload because every account needs its own password hash.")
        };

        public override async Task StageAsync(AppDbContext db, IReadOnlyList<BulkRow> rows, BulkContext ctx)
        {
            var usernames = (await db.Users.Select(u => u.Username).ToListAsync()).Select(u => u.ToLowerInvariant()).ToHashSet();
            var deputations = (await db.Users.Where(u => u.DeputationNumber != null).Select(u => u.DeputationNumber!).ToListAsync()).ToHashSet();

            foreach (var r in rows)
            {
                var username = r.Required("Username", 100);
                var fullName = r.Required("Full Name", 150);
                var role = r.RequiredChoice<Role>("Role");
                var deputation = r.Text("Deputation Number", 50);
                var contact = r.Text("Contact Number", 30);
                var email = r.Text("Email", 150);
                var address = r.Text("Address", 300);
                var marital = r.Choice<MaritalStatus>("Marital Status");
                var active = r.Bool("Is Active", true);

                if (username.Contains(' ')) r.Error("Username cannot contain spaces.");
                if (email != null && !IsEmail(email)) r.Error("Email address is not valid.");
                if (role == Role.ApprehendingOfficer && deputation == null) r.Error("Deputation Number is required for an ApprehendingOfficer.");
                if (r.HasErrors) continue;

                if (!usernames.Add(username.ToLowerInvariant())) { ctx.Count("Users skipped (username exists)"); continue; }

                if (role != Role.ApprehendingOfficer) deputation = null;
                else if (!deputations.Add(deputation!))
                {
                    r.Error($"Deputation Number {deputation} is already assigned to another officer.");
                    continue;
                }

                var password = PasswordGenerator.GenerateTemporary();
                db.Users.Add(new User
                {
                    Username = username,
                    FullName = fullName,
                    Role = role,
                    DeputationNumber = deputation,
                    ContactNumber = contact,
                    Email = email,
                    Address = address,
                    MaritalStatus = marital,
                    IsActive = active,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    MustChangePassword = true
                });
                ctx.Result.Credentials.Add(new BulkImportCredentialDTO { Username = username, TemporaryPassword = password });
                ctx.Count("Users added");
            }
        }

        private static bool IsEmail(string s)
        {
            try { return new MailAddress(s).Address == s; } catch { return false; }
        }
    }

    // ───────────────────────── Violators ─────────────────────────
    public sealed class ViolatorBulkModule : BulkModule
    {
        public override string Key => "violators";
        public override string Title => "Violators";
        public override string SheetName => "Violators";

        public override string? Prerequisites => "Upload the Barangays sheet first if you use the Barangay column - barangays are matched by name.";

        public override IReadOnlyList<BulkColumn> Columns { get; } = new[]
        {
            new BulkColumn("Last Name *", true, "Required. Up to 100 characters."),
            new BulkColumn("First Name *", true, "Required. Up to 100 characters."),
            new BulkColumn("Middle Name", false, "Optional. Up to 100 characters."),
            new BulkColumn("Suffix", false, "Optional. e.g. Jr., III. Up to 20 characters."),
            new BulkColumn("Gender *", true, "Required. Male or Female.", Opt.Of<Gender>()),
            new BulkColumn("Birthday", false, "Optional. yyyy-mm-dd. Not in the future.", null, ColType.Date),
            new BulkColumn("Address", false, "Optional. Street address, up to 300 characters."),
            new BulkColumn("Barangay", false, "Optional. Home barangay (Pasig residents). Must match a barangay name already in POVIS."),
            new BulkColumn("City", false, "Optional. City of residence for non-Pasig residents. Up to 100 characters."),
            new BulkColumn("Presented Valid ID", false, "Optional. e.g. DRIVER'S LICENSE, NATIONAL ID. Up to 100 characters."),
            new BulkColumn("Contact Number", false, "Optional. Up to 30 characters.")
        };

        public override IReadOnlyList<string[]> Examples { get; } = new[]
        {
            new[] { "Dela Cruz", "Juan", "Santos", "Jr.", "Male", "1990-05-14", "12 Rizal St.", "Ugong", "", "DRIVER'S LICENSE", "09171234567" },
            new[] { "Reyes", "Maria", "", "", "Female", "", "5 Mabini Ave.", "", "Marikina", "", "" }
        };

        public override IReadOnlyList<(string, string)> Notes { get; } = new[]
        {
            ("Existing records", "A violator with the same Last Name + First Name + Middle Name + Birthday as an existing one (or an earlier row) is skipped, not changed."),
            ("Violations sheet", "You can skip this sheet when uploading Violations - the Violations template can create violators from the name columns.")
        };

        public override async Task StageAsync(AppDbContext db, IReadOnlyList<BulkRow> rows, BulkContext ctx)
        {
            var barangays = (await db.Barangays.ToListAsync()).GroupBy(b => b.Name.Trim().ToLowerInvariant()).ToDictionary(g => g.Key, g => g.First().Id);
            var seen = (await db.Violators.Select(v => new { v.LastName, v.FirstName, v.MiddleName, v.Birthday }).ToListAsync())
                .Select(v => PersonKey(v.LastName, v.FirstName, v.MiddleName, v.Birthday)).ToHashSet();
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(8));

            foreach (var r in rows)
            {
                var last = r.Required("Last Name", 100);
                var first = r.Required("First Name", 100);
                var middle = r.Text("Middle Name", 100);
                var suffix = r.Text("Suffix", 20);
                var gender = r.RequiredChoice<Gender>("Gender");
                var birthday = r.Date("Birthday");
                var address = r.Text("Address", 300);
                var barangayName = r.Text("Barangay");
                var city = r.Text("City", 100);
                var validId = r.Text("Presented Valid ID", 100);
                var contact = r.Text("Contact Number", 30);

                if (birthday > today) r.Error("Birthday cannot be in the future.");
                Guid? barangayId = null;
                if (barangayName != null)
                {
                    if (barangays.TryGetValue(barangayName.ToLowerInvariant(), out var id)) barangayId = id;
                    else r.Error($"Barangay '{barangayName}' does not exist. Upload it in Barangays first or fix the spelling.");
                }
                if (r.HasErrors) continue;

                if (!seen.Add(PersonKey(last, first, middle, birthday))) { ctx.Count("Violators skipped (already exist)"); continue; }
                db.Violators.Add(new Violator
                {
                    LastName = last, FirstName = first, MiddleName = middle, Suffix = suffix, Gender = gender, Birthday = birthday,
                    Address = address, BarangayId = barangayId, City = city, PresentedValidId = validId, ContactNumber = contact
                });
                ctx.Count("Violators added");
            }
        }

        internal static string PersonKey(string last, string first, string? middle, DateOnly? birthday) =>
            $"{last.Trim().ToLowerInvariant()}|{first.Trim().ToLowerInvariant()}|{(middle ?? "").Trim().ToLowerInvariant()}|{birthday:yyyy-MM-dd}";
    }
}
