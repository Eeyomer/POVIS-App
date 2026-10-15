using Microsoft.EntityFrameworkCore;
using POVISAPI.Data;
using POVISAPI.Models.Entity;
using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Services.BulkImport
{
    public sealed class ViolationBulkModule : BulkModule
    {
        public override string Key => "violations";
        public override string Title => "Violations";
        public override string SheetName => "Violations";
        public override int MaxRows => 10000;

        public override string? Prerequisites =>
            "Upload order: Barangays and Ordinances first (they are matched by name / number). Violators are matched by name + birthday and created automatically when not found.";

        public override IReadOnlyList<BulkColumn> Columns { get; } = new[]
        {
            new BulkColumn("OVR Number *", true, "Required. Ticket number, up to 20 characters. Keep leading zeros (column is formatted as text), e.g. 027620."),
            new BulkColumn("Ordinance Number *", true, "Required. Must match an ordinance already in POVIS, e.g. 19, 07, PCG-60."),
            new BulkColumn("Series *", true, "Required. Year of the ordinance series, e.g. 2025."),
            new BulkColumn("Section Code", false, "Optional. Must match a section of that ordinance, e.g. \"Section 21\". Leave blank if none."),
            new BulkColumn("Last Name *", true, "Required. Violator's last name."),
            new BulkColumn("First Name *", true, "Required. Violator's first name."),
            new BulkColumn("Middle Name", false, "Optional. Helps tell people with the same name apart."),
            new BulkColumn("Suffix", false, "Optional. Used only when a new violator has to be created."),
            new BulkColumn("Birthday", false, "Optional. yyyy-mm-dd. Used to match the right violator and to compute the age at apprehension.", null, ColType.Date),
            new BulkColumn("Gender", false, "Male or Female. Required only when the violator does not exist yet.", Opt.Of<Gender>()),
            new BulkColumn("Date Of Occurrence *", true, "Required. Date of apprehension, yyyy-mm-dd.", null, ColType.Date),
            new BulkColumn("Time Of Occurrence", false, "Optional. hh:mm (24-hour).", null, ColType.Time),
            new BulkColumn("Date Of Submission", false, "Optional. Date the ticket was submitted/encoded. Defaults to the Date Of Occurrence.", null, ColType.Date),
            new BulkColumn("Division *", true, "Required. Apprehending unit: POD, BPD, ALD, PSD, PNP, SOG or Other.", Opt.Of<Division>()),
            new BulkColumn("Age At Apprehension", false, "Optional. Whole number 1-120. Ignored when a Birthday is given (age is computed).", null, ColType.Number),
            new BulkColumn("Place Of Occurrence", false, "Optional. Up to 300 characters."),
            new BulkColumn("Barangay Of Occurrence", false, "Optional. Must match a barangay name already in POVIS. Drives the per-barangay reports."),
            new BulkColumn("Witness", false, "Optional. Up to 150 characters."),
            new BulkColumn("Apprehending Officer", false, "Optional. Name, up to 150 characters. Used by the incentive reports."),
            new BulkColumn("Apprehending Officer Deputation Number", false, "Optional. Up to 50 characters."),
            new BulkColumn("Is Warning Ticket", false, "Yes or No. Default No.", Opt.YesNo),
            new BulkColumn("Offense Level", false, "First, Second, Third or Fourth. Leave blank to have it computed from the violator's earlier tickets for the same ordinance (in date order).", Opt.Of<OffenseLevel>()),
            new BulkColumn("Status", false, "Optional. Pending, Paid, ForSeminar, ForCounseling, ForCommunityService, CaseFiled, Dismissed, Closed, DidNotComply or WarningIssued. Default Pending (WarningIssued for warning tickets).", Opt.Of<ViolationStatus>()),
            new BulkColumn("Remarks", false, "Optional. Up to 500 characters."),
            new BulkColumn("Remarks 2", false, "Optional. Up to 500 characters."),
            new BulkColumn("Is Reviewed", false, "Yes or No. Default No. Yes marks the ticket as already reviewed by the OVR Supervisor.", Opt.YesNo)
        };

        public override IReadOnlyList<string[]> Examples { get; } = new[]
        {
            new[] { "027620", "19", "2025", "Section 21", "Dela Cruz", "Juan", "Santos", "Jr.", "1990-05-14", "Male", "2026-06-02", "14:30", "2026-06-03", "BPD", "", "Plaza Rizal", "Ugong", "", "ROGER ALCANTARA JR", "2023-02-010", "No", "", "Pending", "", "", "No" },
            new[] { "027621", "07", "2019", "", "Reyes", "Maria", "", "", "", "Female", "2026-06-05", "", "", "PNP", "27", "Market area", "Santolan", "", "", "", "No", "", "", "", "", "No" }
        };

        public override IReadOnlyList<(string, string)> Notes { get; } = new[]
        {
            ("Violators", "Rows are matched to an existing violator by Last Name + First Name (+ Middle Name, + Birthday when given). If none matches, a new violator is created (Gender required). If several match, add the Birthday or Middle Name to say which one."),
            ("Offense level", "When Offense Level is blank it is computed per violator and ordinance from earlier tickets, both already in POVIS and earlier dates in this file. Warning tickets and dismissed tickets do not count."),
            ("Existing records", "A ticket with the same OVR Number + ordinance that already exists is skipped, not changed."),
            ("Same OVR, two violations", "If one OVR ticket covers two ordinances, use one row per ordinance with the same OVR Number."),
            ("Next steps", "Payments, seminars, counseling, community service and filed cases are uploaded in their own templates and refer to a ticket by OVR Number.")
        };

        public override async Task StageAsync(AppDbContext db, IReadOnlyList<BulkRow> rows, BulkContext ctx)
        {
            var ordinances = (await db.Ordinances.Include(o => o.Sections).ToListAsync())
                .GroupBy(o => (o.OrdinanceNumber.ToLowerInvariant(), o.Series)).ToDictionary(g => g.Key, g => g.First());
            var barangays = (await db.Barangays.ToListAsync())
                .GroupBy(b => b.Name.Trim().ToLowerInvariant()).ToDictionary(g => g.Key, g => g.First().Id);

            // Violators grouped by "last|first" so rows can be matched cheaply.
            var violatorsByName = (await db.Violators.AsNoTracking()
                    .Select(v => new Candidate(v.Id, v.LastName, v.FirstName, v.MiddleName, v.Birthday)).ToListAsync())
                .GroupBy(v => NameKey(v.Last, v.First)).ToDictionary(g => g.Key, g => g.ToList());

            var existingTickets = (await db.Violations.Select(v => new { v.OvrNumber, v.OrdinanceId }).ToListAsync())
                .Select(v => (v.OvrNumber.ToLowerInvariant(), v.OrdinanceId)).ToHashSet();

            // Prior non-warning, non-dismissed tickets per violator + ordinance (offense level history).
            var history = (await db.Violations.Where(v => !v.IsWarningTicket && v.Status != ViolationStatus.Dismissed)
                    .GroupBy(v => new { v.ViolatorId, v.OrdinanceId })
                    .Select(g => new { g.Key.ViolatorId, g.Key.OrdinanceId, Count = g.Count() }).ToListAsync())
                .ToDictionary(x => (x.ViolatorId, x.OrdinanceId), x => x.Count);

            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(8));

            // ── Pass 1: validate + resolve every row ────────────────────────────
            var parsed = new List<Parsed>();
            foreach (var r in rows)
            {
                var p = new Parsed { Row = r };
                p.Ovr = r.Required("OVR Number", 20);
                var ordNumber = r.Required("Ordinance Number", 20);
                var series = r.Int("Series", 1900, 2100);
                if (series == null && !r.HasErrors) r.Error("Series is required.");
                var sectionCode = r.Text("Section Code", 50);
                p.Last = r.Required("Last Name", 100);
                p.First = r.Required("First Name", 100);
                p.Middle = r.Text("Middle Name", 100);
                p.Suffix = r.Text("Suffix", 20);
                p.Birthday = r.Date("Birthday");
                p.Gender = r.Choice<Gender>("Gender");
                p.Occurrence = r.RequiredDate("Date Of Occurrence");
                p.Time = r.Time("Time Of Occurrence");
                p.Submission = r.Date("Date Of Submission");
                p.Division = r.RequiredChoice<Division>("Division");
                var age = r.Int("Age At Apprehension", 1, 120);
                p.Place = r.Text("Place Of Occurrence", 300);
                var barangayName = r.Text("Barangay Of Occurrence");
                p.Witness = r.Text("Witness", 150);
                p.Officer = r.Text("Apprehending Officer", 150);
                p.OfficerDeputation = r.Text("Apprehending Officer Deputation Number", 50);
                p.Warning = r.Bool("Is Warning Ticket");
                p.Level = r.Choice<OffenseLevel>("Offense Level");
                p.Status = r.Choice<ViolationStatus>("Status");
                p.Remarks = r.Text("Remarks", 500);
                p.Remarks2 = r.Text("Remarks 2", 500);
                p.Reviewed = r.Bool("Is Reviewed");

                if (p.Birthday > today) r.Error("Birthday cannot be in the future.");
                if (p.Birthday > p.Occurrence) r.Error("Birthday is after the Date Of Occurrence.");
                if (p.Occurrence > today) r.Error("Date Of Occurrence cannot be in the future.");

                if (!r.HasErrors)
                {
                    if (!ordinances.TryGetValue((ordNumber.ToLowerInvariant(), series!.Value), out var ordinance))
                        r.Error($"Ordinance {ordNumber} S.{series} does not exist. Upload it in Ordinances first.");
                    else
                    {
                        p.Ordinance = ordinance;
                        if (sectionCode != null)
                        {
                            var section = ordinance.Sections.FirstOrDefault(s => string.Equals(s.SectionCode, sectionCode, StringComparison.OrdinalIgnoreCase));
                            if (section == null) r.Error($"Section '{sectionCode}' does not exist under ordinance {ordNumber} S.{series}.");
                            else p.SectionId = section.Id;
                        }
                    }
                    if (barangayName != null)
                    {
                        if (barangays.TryGetValue(barangayName.ToLowerInvariant(), out var bid)) p.BarangayId = bid;
                        else r.Error($"Barangay '{barangayName}' does not exist. Upload it in Barangays first or fix the spelling.");
                    }
                    p.Age = age;
                }
                if (!r.HasErrors) parsed.Add(p);
            }
            if (ctx.ErrorCount > 0) return;

            // ── Pass 2: stage in chronological order so offense levels come out right ──
            var violatorIds = new Dictionary<string, Guid>(); // key of violators created in this file
            var added = new HashSet<(string, Guid)>();

            foreach (var p in parsed.OrderBy(x => x.Occurrence).ThenBy(x => x.Row.Number))
            {
                var r = p.Row;
                var ticketKey = (p.Ovr.ToLowerInvariant(), p.Ordinance!.Id);
                if (existingTickets.Contains(ticketKey) || !added.Add(ticketKey))
                {
                    ctx.Count("Violations skipped (OVR + ordinance already exist)");
                    continue;
                }

                // Find or create the violator.
                Guid violatorId;
                var nameKey = NameKey(p.Last, p.First);
                var newKey = VKey(p);
                if (violatorIds.TryGetValue(newKey, out var created)) violatorId = created;
                else
                {
                    var matches = violatorsByName.TryGetValue(nameKey, out var list)
                        ? list.Where(c => Same(c.Middle, p.Middle, p.Middle != null) && (p.Birthday == null || c.Birthday == null || c.Birthday == p.Birthday)).ToList()
                        : new List<Candidate>();
                    if (matches.Count > 1)
                    {
                        r.Error($"{p.Last}, {p.First} matches {matches.Count} existing violators. Add the Birthday or Middle Name to tell which one.");
                        continue;
                    }
                    if (matches.Count == 1) violatorId = matches[0].Id;
                    else
                    {
                        if (p.Gender == null) { r.Error($"Gender is required because violator {p.Last}, {p.First} does not exist yet."); continue; }
                        var violator = new Violator
                        {
                            LastName = p.Last, FirstName = p.First, MiddleName = p.Middle, Suffix = p.Suffix,
                            Gender = p.Gender.Value, Birthday = p.Birthday
                        };
                        db.Violators.Add(violator);
                        violatorId = violator.Id;
                        violatorIds[newKey] = violatorId;
                        ctx.Count("Violators created");
                    }
                }

                var histKey = (violatorId, p.Ordinance.Id);
                history.TryGetValue(histKey, out var prior);
                var level = p.Level ?? prior switch { 0 => OffenseLevel.First, 1 => OffenseLevel.Second, 2 => OffenseLevel.Third, _ => OffenseLevel.Fourth };
                var status = p.Status ?? (p.Warning ? ViolationStatus.WarningIssued : ViolationStatus.Pending);
                if (!p.Warning && status != ViolationStatus.Dismissed) history[histKey] = prior + 1;

                db.Violations.Add(new Violation
                {
                    ViolatorId = violatorId,
                    OvrNumber = p.Ovr,
                    DateOfSubmission = p.Submission ?? p.Occurrence,
                    Division = p.Division,
                    AgeAtApprehension = Age(p.Birthday, p.Occurrence) ?? p.Age,
                    DateOfOccurrence = p.Occurrence,
                    TimeOfOccurrence = p.Time,
                    PlaceOfOccurrence = p.Place,
                    BarangayOfOccurrenceId = p.BarangayId,
                    Witness = p.Witness,
                    ApprehendingOfficer = p.Officer,
                    ApprehendingOfficerDeputationNumber = p.OfficerDeputation,
                    OrdinanceId = p.Ordinance.Id,
                    OrdinanceSectionId = p.SectionId,
                    OffenseLevel = level,
                    IsWarningTicket = p.Warning,
                    Status = status,
                    Remarks = p.Remarks,
                    Remarks2 = p.Remarks2,
                    IsReviewed = p.Reviewed,
                    ReviewedAt = p.Reviewed ? DateTime.UtcNow : null,
                    ReviewedBy = p.Reviewed ? ctx.Uploader : null
                });
                ctx.Count("Violations added");
            }
        }

        private sealed record Candidate(Guid Id, string Last, string First, string? Middle, DateOnly? Birthday);

        private sealed class Parsed
        {
            public BulkRow Row = null!;
            public string Ovr = "", Last = "", First = "";
            public string? Middle, Suffix, Place, Witness, Officer, OfficerDeputation, Remarks, Remarks2;
            public Ordinance? Ordinance;
            public Guid? SectionId, BarangayId;
            public DateOnly? Birthday, Submission;
            public DateOnly Occurrence;
            public TimeOnly? Time;
            public Gender? Gender;
            public Division Division;
            public int? Age;
            public bool Warning, Reviewed;
            public OffenseLevel? Level;
            public ViolationStatus? Status;
        }

        private static string NameKey(string last, string first) => $"{last.Trim().ToLowerInvariant()}|{first.Trim().ToLowerInvariant()}";
        private static string VKey(Parsed p) => ViolatorBulkModule.PersonKey(p.Last, p.First, p.Middle, p.Birthday);

        // When the row gives a middle name it must match; when it doesn't, any candidate is acceptable.
        private static bool Same(string? a, string? b, bool compare) =>
            !compare || string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase);

        private static int? Age(DateOnly? birthday, DateOnly occurrence)
        {
            if (birthday == null || birthday > occurrence) return null;
            int age = occurrence.Year - birthday.Value.Year;
            if (occurrence < birthday.Value.AddYears(age)) age--;
            return age;
        }
    }
}
