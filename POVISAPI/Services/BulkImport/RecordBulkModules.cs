using Microsoft.EntityFrameworkCore;
using POVISAPI.Data;
using POVISAPI.Models.Entity;
using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Services.BulkImport
{
    /// <summary>Common columns + lookup for every sheet that attaches a record to an existing violation by OVR number.</summary>
    public abstract class RecordBulkModule : BulkModule
    {
        protected static readonly BulkColumn[] TicketColumns =
        {
            new BulkColumn("OVR Number *", true, "Required. The OVR ticket number of an existing violation (exact match, keep leading zeros)."),
            new BulkColumn("Ordinance Number", false, "Only needed when the same OVR Number belongs to more than one violation (two ordinances on one ticket)."),
            new BulkColumn("Series", false, "Year of that ordinance. Give both Ordinance Number and Series, or neither.")
        };

        protected static BulkColumn[] WithTicket(params BulkColumn[] rest) => TicketColumns.Concat(rest).ToArray();

        public override string? Prerequisites =>
            "Upload the Violations sheet first - each row here is attached to an existing violation by its OVR Number.";

        protected static readonly (string, string) StatusRule = ("Violation status",
            "Adding a record moves the violation to the matching status (like adding it in the app): Completed -> Closed, DidNotComply -> DidNotComply, otherwise For Seminar / Counseling / Community Service. Violations that are Paid, Closed, Case Filed or Dismissed are not moved back.");

        protected sealed class Ticket
        {
            public BulkRow Row = null!;
            public Violation Violation = null!;
        }

        /// <summary>Loads the violations referenced by the rows and returns, per row, the one it points at (or records an error).</summary>
        protected static async Task<List<Ticket>> ResolveAsync(AppDbContext db, IReadOnlyList<BulkRow> rows,
            Func<IQueryable<Violation>, IQueryable<Violation>> include)
        {
            var parsed = new List<(BulkRow Row, string Ovr, string? Ordinance, int? Series)>();
            foreach (var r in rows)
            {
                var ovr = r.Required("OVR Number", 20);
                var ord = r.Text("Ordinance Number", 20);
                var series = r.Int("Series", 1900, 2100);
                if ((ord == null) != (series == null) && !r.HasErrors) r.Error("Give both Ordinance Number and Series, or neither.");
                parsed.Add((r, ovr, ord, series));
            }

            var ovrs = parsed.Where(p => !p.Row.HasErrors).Select(p => p.Ovr).Distinct().ToList();
            var violations = ovrs.Count == 0 ? new List<Violation>() : await include(
                    db.Violations.Include(v => v.Ordinance).Where(v => ovrs.Contains(v.OvrNumber)))
                .ToListAsync();
            var byOvr = violations.GroupBy(v => v.OvrNumber.ToLowerInvariant()).ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<Ticket>();
            foreach (var p in parsed)
            {
                if (p.Row.HasErrors) continue;
                if (!byOvr.TryGetValue(p.Ovr.ToLowerInvariant(), out var matches))
                {
                    p.Row.Error($"No violation with OVR Number {p.Ovr} exists. Upload it in Violations first.");
                    continue;
                }
                if (p.Ordinance != null)
                    matches = matches.Where(v => string.Equals(v.Ordinance.OrdinanceNumber, p.Ordinance, StringComparison.OrdinalIgnoreCase) && v.Ordinance.Series == p.Series).ToList();
                if (matches.Count == 0) { p.Row.Error($"OVR {p.Ovr} has no violation under ordinance {p.Ordinance} S.{p.Series}."); continue; }
                if (matches.Count > 1) { p.Row.Error($"OVR {p.Ovr} belongs to {matches.Count} violations. Fill in Ordinance Number and Series to pick one."); continue; }
                result.Add(new Ticket { Row = p.Row, Violation = matches[0] });
            }
            return result;
        }

        /// <summary>Moves a violation to a new status without undoing a settled/closed one.</summary>
        protected static void MoveStatus(Violation v, ViolationStatus target)
        {
            if (v.Status == ViolationStatus.Dismissed) return;
            var progressing = target is ViolationStatus.ForSeminar or ViolationStatus.ForCounseling or ViolationStatus.ForCommunityService;
            if (progressing && v.Status is ViolationStatus.Paid or ViolationStatus.Closed or ViolationStatus.CaseFiled or ViolationStatus.DidNotComply) return;
            v.Status = target;
        }

        protected static ViolationStatus StatusFor(ComplianceStatus s, ViolationStatus whenOpen) => s switch
        {
            ComplianceStatus.Completed => ViolationStatus.Closed,
            ComplianceStatus.DidNotComply => ViolationStatus.DidNotComply,
            _ => whenOpen
        };

        protected static readonly string[] ComplianceOptions = Opt.Of<ComplianceStatus>();
    }

    // ───────────────────────── Payments ─────────────────────────
    public sealed class PaymentBulkModule : RecordBulkModule
    {
        public override string Key => "payments";
        public override string Title => "Payments";
        public override string SheetName => "Payments";
        public override int MaxRows => 10000;

        public override IReadOnlyList<BulkColumn> Columns { get; } = WithTicket(
            new BulkColumn("Date Of Payment *", true, "Required. yyyy-mm-dd. Not in the future.", null, ColType.Date),
            new BulkColumn("Amount *", true, "Required. Pesos, 0 to 99,999,999. Total paid for the ticket.", null, ColType.Number),
            new BulkColumn("Receipt Number", false, "Optional. Official receipt / PC number, up to 50 characters."),
            new BulkColumn("Paid At", false, "Optional. Where it was paid (e.g. CENRO, Treasurer's Office). Up to 100 characters."),
            new BulkColumn("Remarks", false, "Optional. Up to 500 characters."));

        public override IReadOnlyList<string[]> Examples { get; } = new[]
        {
            new[] { "027620", "", "", "2026-06-10", "500", "AA 2447378", "CENRO", "" },
            new[] { "027621", "07", "2019", "2026-06-12", "1000", "", "Treasurer's Office", "Second offense" }
        };

        public override IReadOnlyList<(string, string)> Notes { get; } = new[]
        {
            ("One payment per ticket", "A violation that already has a payment is skipped, not changed."),
            ("Violation status", "Adding a payment sets the violation to Paid (or Closed if a case was filed), like recording it in the app.")
        };

        public override async Task StageAsync(AppDbContext db, IReadOnlyList<BulkRow> rows, BulkContext ctx)
        {
            var tickets = await ResolveAsync(db, rows, q => q.Include(v => v.Payment).Include(v => v.FiledCase));
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(8));
            var seen = new HashSet<Guid>();

            foreach (var t in tickets)
            {
                var r = t.Row;
                var date = r.RequiredDate("Date Of Payment");
                var amount = r.Decimal("Amount", 0, 99_999_999);
                if (amount == null && !r.HasErrors) r.Error("Amount is required.");
                var receipt = r.Text("Receipt Number", 50);
                var paidAt = r.Text("Paid At", 100);
                var remarks = r.Text("Remarks", 500);
                if (date > today) r.Error("Date Of Payment cannot be in the future.");
                if (r.HasErrors) continue;

                if (t.Violation.Payment != null || !seen.Add(t.Violation.Id)) { ctx.Count("Payments skipped (violation already paid)"); continue; }
                db.Payments.Add(new Payment { ViolationId = t.Violation.Id, DateOfPayment = date, Amount = amount!.Value, ReceiptNumber = receipt, PaidAt = paidAt, Remarks = remarks });
                if (t.Violation.Status != ViolationStatus.Dismissed)
                    t.Violation.Status = t.Violation.FiledCase != null ? ViolationStatus.Closed : ViolationStatus.Paid;
                ctx.Count("Payments added");
            }
        }
    }

    // ───────────────────────── Seminar records ─────────────────────────
    public sealed class SeminarBulkModule : RecordBulkModule
    {
        public override string Key => "seminar-records";
        public override string Title => "Seminar Records";
        public override string SheetName => "Seminar Records";

        public override IReadOnlyList<BulkColumn> Columns { get; } = WithTicket(
            new BulkColumn("Seminar Type *", true, "Required. SmokingCessation, VapingCessation or SolidWasteManagement.", Opt.Of<SeminarType>()),
            new BulkColumn("Scheduled Date", false, "Optional. yyyy-mm-dd. The CENRO seminar schedule.", null, ColType.Date),
            new BulkColumn("Required Days", false, "Optional. 0.5 to 365 (2.5 is allowed).", null, ColType.Number),
            new BulkColumn("Status", false, "Scheduled, Ongoing, Completed or DidNotComply. Default Scheduled.", ComplianceOptions),
            new BulkColumn("Date Completed", false, "Optional. yyyy-mm-dd. Use with Status Completed.", null, ColType.Date),
            new BulkColumn("Remarks", false, "Optional. Up to 500 characters."));

        public override IReadOnlyList<string[]> Examples { get; } = new[]
        {
            new[] { "027620", "", "", "SmokingCessation", "2026-06-20", "5", "Completed", "2026-06-24", "" },
            new[] { "027621", "", "", "VapingCessation", "2026-09-22", "2.5", "Scheduled", "", "" }
        };

        public override IReadOnlyList<(string, string)> Notes { get; } = new[]
        {
            ("Existing records", "A row with the same violation, Seminar Type and Scheduled Date as an existing record is skipped."),
            StatusRule
        };

        public override async Task StageAsync(AppDbContext db, IReadOnlyList<BulkRow> rows, BulkContext ctx)
        {
            var tickets = await ResolveAsync(db, rows, q => q.Include(v => v.SeminarRecords));
            var seen = new HashSet<(Guid, SeminarType, DateOnly?)>();

            foreach (var t in tickets)
            {
                var r = t.Row;
                var type = r.RequiredChoice<SeminarType>("Seminar Type");
                var scheduled = r.Date("Scheduled Date");
                var days = r.Decimal("Required Days", 0.5m, 365);
                var status = r.Choice<ComplianceStatus>("Status") ?? ComplianceStatus.Scheduled;
                var completed = r.Date("Date Completed");
                var remarks = r.Text("Remarks", 500);
                if (status != ComplianceStatus.Completed && completed != null) r.Error("Date Completed is only valid when Status is Completed.");
                if (r.HasErrors) continue;

                if (t.Violation.SeminarRecords.Any(s => s.SeminarType == type && s.ScheduledDate == scheduled) || !seen.Add((t.Violation.Id, type, scheduled)))
                { ctx.Count("Seminar records skipped (already exist)"); continue; }

                db.SeminarRecords.Add(new SeminarRecord
                {
                    ViolationId = t.Violation.Id, SeminarType = type, ScheduledDate = scheduled, RequiredDays = days,
                    Status = status, DateCompleted = completed, Remarks = remarks
                });
                MoveStatus(t.Violation, StatusFor(status, ViolationStatus.ForSeminar));
                ctx.Count("Seminar records added");
            }
        }
    }

    // ───────────────────────── Counseling records ─────────────────────────
    public sealed class CounselingBulkModule : RecordBulkModule
    {
        public override string Key => "counseling-records";
        public override string Title => "Counseling Records";
        public override string SheetName => "Counseling Records";

        public override IReadOnlyList<BulkColumn> Columns { get; } = WithTicket(
            new BulkColumn("Intervention Type", false, "Optional. e.g. Counseling, Assessment + Counseling. Up to 200 characters."),
            new BulkColumn("Conducted By", false, "Optional. e.g. Barangay Council. Up to 150 characters."),
            new BulkColumn("Scheduled Date", false, "Optional. yyyy-mm-dd.", null, ColType.Date),
            new BulkColumn("Status", false, "Scheduled, Ongoing, Completed or DidNotComply. Default Scheduled.", ComplianceOptions),
            new BulkColumn("Date Completed", false, "Optional. yyyy-mm-dd. Use with Status Completed.", null, ColType.Date),
            new BulkColumn("Parent Or Guardian Present", false, "Yes or No. Default No.", Opt.YesNo),
            new BulkColumn("Remarks", false, "Optional. Up to 500 characters."));

        public override IReadOnlyList<string[]> Examples { get; } = new[]
        {
            new[] { "027620", "", "", "Counseling", "Barangay Council", "2026-07-02", "Completed", "2026-07-02", "Yes", "" },
            new[] { "027621", "", "", "Assessment + Counseling", "Barangay Council", "2026-08-10", "Scheduled", "", "No", "" }
        };

        public override IReadOnlyList<(string, string)> Notes { get; } = new[]
        {
            ("Existing records", "A row with the same violation, Intervention Type and Scheduled Date as an existing record is skipped."),
            StatusRule
        };

        public override async Task StageAsync(AppDbContext db, IReadOnlyList<BulkRow> rows, BulkContext ctx)
        {
            var tickets = await ResolveAsync(db, rows, q => q.Include(v => v.CounselingRecords));
            var seen = new HashSet<(Guid, string, DateOnly?)>();

            foreach (var t in tickets)
            {
                var r = t.Row;
                var type = r.Text("Intervention Type", 200);
                var by = r.Text("Conducted By", 150);
                var scheduled = r.Date("Scheduled Date");
                var status = r.Choice<ComplianceStatus>("Status") ?? ComplianceStatus.Scheduled;
                var completed = r.Date("Date Completed");
                var parent = r.Bool("Parent Or Guardian Present");
                var remarks = r.Text("Remarks", 500);
                if (status != ComplianceStatus.Completed && completed != null) r.Error("Date Completed is only valid when Status is Completed.");
                if (r.HasErrors) continue;

                var typeKey = (type ?? "").ToLowerInvariant();
                if (t.Violation.CounselingRecords.Any(c => (c.InterventionType ?? "").ToLowerInvariant() == typeKey && c.ScheduledDate == scheduled) || !seen.Add((t.Violation.Id, typeKey, scheduled)))
                { ctx.Count("Counseling records skipped (already exist)"); continue; }

                db.CounselingRecords.Add(new CounselingRecord
                {
                    ViolationId = t.Violation.Id, InterventionType = type, ConductedBy = by, ScheduledDate = scheduled, Status = status,
                    DateCompleted = completed, ParentOrGuardianPresent = parent, Remarks = remarks
                });
                MoveStatus(t.Violation, StatusFor(status, ViolationStatus.ForCounseling));
                ctx.Count("Counseling records added");
            }
        }
    }

    // ───────────────────────── Community service ─────────────────────────
    public sealed class CommunityServiceBulkModule : RecordBulkModule
    {
        public override string Key => "community-service-records";
        public override string Title => "Community Service Records";
        public override string SheetName => "Community Service";

        public override IReadOnlyList<BulkColumn> Columns { get; } = WithTicket(
            new BulkColumn("Required Hours", false, "Optional. 0.5 to 9,999.", null, ColType.Number),
            new BulkColumn("Required Days", false, "Optional. Whole number 1-365.", null, ColType.Number),
            new BulkColumn("Scheduled Date", false, "Optional. yyyy-mm-dd.", null, ColType.Date),
            new BulkColumn("Status", false, "Scheduled, Ongoing, Completed or DidNotComply. Default Scheduled.", ComplianceOptions),
            new BulkColumn("Date Completed", false, "Optional. yyyy-mm-dd. Use with Status Completed.", null, ColType.Date),
            new BulkColumn("Assigned Location", false, "Optional. Where the service is rendered. Up to 200 characters."),
            new BulkColumn("Remarks", false, "Optional. Up to 500 characters."));

        public override IReadOnlyList<string[]> Examples { get; } = new[]
        {
            new[] { "027620", "", "", "8", "", "2026-07-05", "Completed", "2026-07-05", "Barangay Hall - Ugong", "" },
            new[] { "027621", "", "", "", "15", "2026-08-01", "Scheduled", "", "CENRO", "Third offense" }
        };

        public override IReadOnlyList<(string, string)> Notes { get; } = new[]
        {
            ("Existing records", "A row with the same violation and Scheduled Date as an existing record is skipped."),
            StatusRule
        };

        public override async Task StageAsync(AppDbContext db, IReadOnlyList<BulkRow> rows, BulkContext ctx)
        {
            var tickets = await ResolveAsync(db, rows, q => q.Include(v => v.CommunityServiceRecords));
            var seen = new HashSet<(Guid, DateOnly?)>();

            foreach (var t in tickets)
            {
                var r = t.Row;
                var hours = r.Decimal("Required Hours", 0.5m, 9999);
                var days = r.Int("Required Days", 1, 365);
                var scheduled = r.Date("Scheduled Date");
                var status = r.Choice<ComplianceStatus>("Status") ?? ComplianceStatus.Scheduled;
                var completed = r.Date("Date Completed");
                var location = r.Text("Assigned Location", 200);
                var remarks = r.Text("Remarks", 500);
                if (status != ComplianceStatus.Completed && completed != null) r.Error("Date Completed is only valid when Status is Completed.");
                if (r.HasErrors) continue;

                if (t.Violation.CommunityServiceRecords.Any(c => c.ScheduledDate == scheduled) || !seen.Add((t.Violation.Id, scheduled)))
                { ctx.Count("Community service records skipped (already exist)"); continue; }

                db.CommunityServiceRecords.Add(new CommunityServiceRecord
                {
                    ViolationId = t.Violation.Id, RequiredHours = hours, RequiredDays = days, ScheduledDate = scheduled, Status = status,
                    DateCompleted = completed, AssignedLocation = location, Remarks = remarks
                });
                MoveStatus(t.Violation, StatusFor(status, ViolationStatus.ForCommunityService));
                ctx.Count("Community service records added");
            }
        }
    }

    // ───────────────────────── Filed cases ─────────────────────────
    public sealed class FiledCaseBulkModule : RecordBulkModule
    {
        public override string Key => "filed-cases";
        public override string Title => "Filed Cases";
        public override string SheetName => "Filed Cases";

        public override IReadOnlyList<BulkColumn> Columns { get; } = WithTicket(
            new BulkColumn("Date Filed *", true, "Required. yyyy-mm-dd. The batch filing date.", null, ColType.Date),
            new BulkColumn("NPS Docket Number", false, "Optional. Up to 50 characters."),
            new BulkColumn("Status", false, "Filed, Dismissed or Closed. Default Filed.", Opt.Of<CaseStatus>()),
            new BulkColumn("Date Resolved", false, "Optional. yyyy-mm-dd. Only for Dismissed or Closed cases.", null, ColType.Date),
            new BulkColumn("Resolution Remarks", false, "Optional. e.g. \"CASE CLOSED PAID June 30, 2026\". Up to 500 characters."),
            new BulkColumn("Remarks", false, "Optional. Up to 500 characters."));

        public override IReadOnlyList<string[]> Examples { get; } = new[]
        {
            new[] { "027620", "", "", "2026-06-22", "XVI-INV-26F-01234", "Filed", "", "", "" },
            new[] { "027621", "", "", "2026-06-22", "XVI-INV-26F-01235", "Closed", "2026-06-30", "CASE CLOSED PAID June 30, 2026", "Paid after prosecution decision" }
        };

        public override IReadOnlyList<(string, string)> Notes { get; } = new[]
        {
            ("One case per ticket", "A violation that already has a filed case is skipped, not changed."),
            ("Violation status", "The violation moves to Case Filed, Dismissed or Closed to match the case Status, like in the app. If you also upload Payments, upload Filed Cases first so a paid case ends up Closed.")
        };

        public override async Task StageAsync(AppDbContext db, IReadOnlyList<BulkRow> rows, BulkContext ctx)
        {
            var tickets = await ResolveAsync(db, rows, q => q.Include(v => v.FiledCase));
            var seen = new HashSet<Guid>();

            foreach (var t in tickets)
            {
                var r = t.Row;
                var filed = r.RequiredDate("Date Filed");
                var docket = r.Text("NPS Docket Number", 50);
                var status = r.Choice<CaseStatus>("Status") ?? CaseStatus.Filed;
                var resolved = r.Date("Date Resolved");
                var resolution = r.Text("Resolution Remarks", 500);
                var remarks = r.Text("Remarks", 500);
                if (resolved < filed) r.Error("Date Resolved is before Date Filed.");
                if (status == CaseStatus.Filed && resolved != null) r.Error("Date Resolved is only valid when Status is Dismissed or Closed.");
                if (r.HasErrors) continue;

                if (t.Violation.FiledCase != null || !seen.Add(t.Violation.Id)) { ctx.Count("Filed cases skipped (case already filed)"); continue; }

                db.FiledCases.Add(new FiledCase
                {
                    ViolationId = t.Violation.Id, DateFiled = filed, NpsDocketNumber = docket, Status = status,
                    DateResolved = resolved, ResolutionRemarks = resolution, Remarks = remarks
                });
                t.Violation.Status = status switch
                {
                    CaseStatus.Dismissed => ViolationStatus.Dismissed,
                    CaseStatus.Closed => ViolationStatus.Closed,
                    _ => ViolationStatus.CaseFiled
                };
                ctx.Count("Filed cases added");
            }
        }
    }
}
