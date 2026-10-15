using Microsoft.EntityFrameworkCore;
using POVISAPI.Data;
using POVISAPI.Models.DTO;
using POVISAPI.Models.Entity.Enums;
using POVISAPI.Interfaces;

namespace POVISAPI.Services
{
    public class ReportService : IReportService
    {
        /// <summary>Share of each paid fine that goes to the apprehending deputized enforcer (the 10% column on the INCENTIVES sheets).</summary>
        public const decimal IncentiveRate = 0.10m;

        private readonly AppDbContext _db;
        public ReportService(AppDbContext db) => _db = db;

        // "ORDINANCE NO. 07, S. 2019" / "E.O PCG-60, S. 2022"
        private static string OrdinanceCode(string number, int series, bool isExecutiveOrder) =>
            isExecutiveOrder ? $"E.O {number}, S. {series}" : $"ORDINANCE NO. {number}, S. {series}";

        private static string OffenseSuffix(OffenseLevel level) => level switch
        {
            OffenseLevel.Second => " 2ND OFFENSE",
            OffenseLevel.Third => " 3RD OFFENSE",
            OffenseLevel.Fourth => " 4TH OFFENSE",
            _ => string.Empty
        };

        public async Task<OrdinanceSummaryReportDTO> GetOrdinanceSummaryAsync(DateOnly from, DateOnly to)
        {
            // Pull the slice once, aggregate in memory — row counts here
            // (thousands per half-year) are well within reason for this.
            var slice = await _db.Violations.AsNoTracking()
                .Where(v => v.DateOfOccurrence >= from && v.DateOfOccurrence <= to && !v.IsWarningTicket)
                .Select(v => new
                {
                    v.OrdinanceId,
                    v.OrdinanceSectionId,
                    SectionCode = v.OrdinanceSection != null ? v.OrdinanceSection.SectionCode : null,
                    OrdinanceNumber = v.Ordinance.OrdinanceNumber,
                    v.Ordinance.Series,
                    v.Ordinance.IsExecutiveOrder,
                    OrdinanceShortName = v.Ordinance.ShortName,
                    v.OffenseLevel,
                    v.Violator.Gender,
                    v.Status,
                    PaidAmount = v.Payment != null ? v.Payment.Amount : (decimal?)null,
                    HasSeminar = v.SeminarRecords.Any(),
                    HasCounseling = v.CounselingRecords.Any(),
                    HasCommunityService = v.CommunityServiceRecords.Any(),
                    // "unpaid" per the OVERALL sheet = fine amount of unsettled violations
                    FineAmount = _db.OrdinancePenalties
                        .Where(p => p.OrdinanceId == v.OrdinanceId
                                 && p.OffenseLevel == v.OffenseLevel
                                 && (p.OrdinanceSectionId == v.OrdinanceSectionId || p.OrdinanceSectionId == null))
                        .OrderByDescending(p => p.OrdinanceSectionId != null)
                        .Select(p => p.FineAmount)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // One row per ordinance (+ section); 2nd/3rd/4th offenses get their own row, like the sheet.
            var groups = slice
                .GroupBy(x => new { x.OrdinanceId, x.OrdinanceSectionId, x.OffenseLevel })
                .Select(g =>
                {
                    var first = g.First();
                    var label = OrdinanceCode(first.OrdinanceNumber, first.Series, first.IsExecutiveOrder)
                              + " " + first.OrdinanceShortName.ToUpperInvariant()
                              + (first.SectionCode != null ? $" ({first.SectionCode})" : string.Empty)
                              + OffenseSuffix(g.Key.OffenseLevel);

                    return new
                    {
                        g.Key.OrdinanceId,
                        g.Key.OrdinanceSectionId,
                        g.Key.OffenseLevel,
                        Row = new OrdinanceSummaryRowDTO
                        {
                            OrdinanceId = $"{g.Key.OrdinanceId}:{g.Key.OrdinanceSectionId}:{(int)g.Key.OffenseLevel}",
                            OrdinanceShortName = first.OrdinanceShortName,
                            Label = label,
                            FemaleCount = g.Count(x => x.Gender == Gender.Female),
                            MaleCount = g.Count(x => x.Gender == Gender.Male),
                            TotalViolators = g.Count(),
                            SeminarCount = g.Count(x => x.HasSeminar),
                            CounselingCount = g.Count(x => x.HasCounseling),
                            CommunityServiceCount = g.Count(x => x.HasCommunityService),
                            DidNotComplyCount = g.Count(x => x.Status == ViolationStatus.DidNotComply || x.Status == ViolationStatus.Pending),
                            PaidCount = g.Count(x => x.PaidAmount.HasValue),
                            FineAmount = first.FineAmount,
                            TotalAmountPaid = g.Sum(x => x.PaidAmount ?? 0m),
                            TotalAmountUnpaid = g.Where(x => !x.PaidAmount.HasValue
                                                          && x.Status != ViolationStatus.Dismissed
                                                          && x.Status != ViolationStatus.Closed)
                                                 .Sum(x => x.FineAmount ?? 0m)
                        }
                    };
                })
                .ToList();

            var groupTotals = groups
                .GroupBy(x => new { x.OrdinanceId, x.OrdinanceSectionId })
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Row.TotalViolators));

            var rows = groups
                .OrderByDescending(x => groupTotals[new { x.OrdinanceId, x.OrdinanceSectionId }])
                .ThenBy(x => x.OrdinanceId)
                .ThenBy(x => x.OrdinanceSectionId)
                .ThenBy(x => x.OffenseLevel)
                .Select(x => x.Row)
                .ToList();

            return new OrdinanceSummaryReportDTO
            {
                From = from,
                To = to,
                Rows = rows,
                TotalFemale = rows.Sum(r => r.FemaleCount),
                TotalMale = rows.Sum(r => r.MaleCount),
                TotalViolators = rows.Sum(r => r.TotalViolators),
                TotalSeminar = rows.Sum(r => r.SeminarCount),
                TotalCounseling = rows.Sum(r => r.CounselingCount),
                TotalCommunityService = rows.Sum(r => r.CommunityServiceCount),
                TotalDidNotComply = rows.Sum(r => r.DidNotComplyCount),
                TotalPaid = rows.Sum(r => r.PaidCount),
                TotalAmountPaid = rows.Sum(r => r.TotalAmountPaid),
                TotalAmountUnpaid = rows.Sum(r => r.TotalAmountUnpaid)
            };
        }

        public async Task<PerBarangayReportDTO> GetPerBarangayAsync(DateOnly from, DateOnly to)
        {
            var counts = await _db.Violations.AsNoTracking()
                .Where(v => v.DateOfOccurrence >= from && v.DateOfOccurrence <= to
                         && v.BarangayOfOccurrenceId != null && !v.IsWarningTicket)
                .GroupBy(v => new
                {
                    BarangayId = v.BarangayOfOccurrenceId!.Value,
                    BarangayName = v.BarangayOfOccurrence!.Name,
                    v.BarangayOfOccurrence.District,
                    OrdinanceName = v.Ordinance.ShortName
                })
                .Select(g => new { g.Key.BarangayId, g.Key.BarangayName, g.Key.District, g.Key.OrdinanceName, Count = g.Count() })
                .ToListAsync();

            var columns = counts.Select(c => c.OrdinanceName).Distinct().OrderBy(c => c).ToList();
            var grandTotal = counts.Sum(c => c.Count);

            var rows = counts
                .GroupBy(c => new { c.BarangayId, c.BarangayName, c.District })
                .Select(g =>
                {
                    var total = g.Sum(x => x.Count);
                    return new BarangayOrdinanceRowDTO
                    {
                        BarangayId = g.Key.BarangayId.ToString(),
                        BarangayName = g.Key.BarangayName,
                        District = g.Key.District,
                        CountsByOrdinance = g.ToDictionary(x => x.OrdinanceName, x => x.Count),
                        Total = total,
                        Share = grandTotal == 0 ? 0 : (double)total / grandTotal
                    };
                })
                .OrderBy(r => r.District).ThenBy(r => r.BarangayName)
                .ToList();

            return new PerBarangayReportDTO
            {
                From = from,
                To = to,
                OrdinanceColumns = columns,
                Rows = rows,
                TotalsByOrdinance = columns.ToDictionary(c => c, c => counts.Where(x => x.OrdinanceName == c).Sum(x => x.Count)),
                GrandTotal = grandTotal
            };
        }

        public async Task<SeminarBarangayReportDTO> GetSeminarPerBarangayAsync(DateOnly from, DateOnly to, Guid? barangayId)
        {
            var query = _db.Violations.AsNoTracking()
                .Where(v => v.DateOfOccurrence >= from && v.DateOfOccurrence <= to
                         && v.BarangayOfOccurrenceId != null && !v.IsWarningTicket);
            if (barangayId.HasValue)
                query = query.Where(v => v.BarangayOfOccurrenceId == barangayId);

            var slice = await query
                .Select(v => new
                {
                    BarangayId = v.BarangayOfOccurrenceId!.Value,
                    BarangayName = v.BarangayOfOccurrence!.Name,
                    v.DateOfOccurrence,
                    v.Division,
                    v.Violator.Gender,
                    v.AgeAtApprehension,
                    v.OvrNumber,
                    v.PlaceOfOccurrence,
                    OrdinanceNumber = v.Ordinance.OrdinanceNumber,
                    v.Ordinance.Series,
                    v.Ordinance.IsExecutiveOrder,
                    OrdinanceShortName = v.Ordinance.ShortName,
                    SectionCode = v.OrdinanceSection != null ? v.OrdinanceSection.SectionCode : null,
                    v.Status,
                    v.Remarks,
                    v.ApprehendingOfficer,
                    v.ApprehendingOfficerDeputationNumber,
                    PaymentDate = v.Payment != null ? v.Payment.DateOfPayment : (DateOnly?)null,
                    CompletedSeminar = v.SeminarRecords
                        .Where(s => s.Status == ComplianceStatus.Completed)
                        .Select(s => new { s.SeminarType, s.DateCompleted }).FirstOrDefault(),
                    CompletedCommunityService = v.CommunityServiceRecords
                        .Where(s => s.Status == ComplianceStatus.Completed)
                        .Select(s => s.DateCompleted).FirstOrDefault(),
                    CompletedCounseling = v.CounselingRecords
                        .Where(s => s.Status == ComplianceStatus.Completed)
                        .Select(s => s.DateCompleted).FirstOrDefault(),
                    HasCompletedCommunityService = v.CommunityServiceRecords.Any(s => s.Status == ComplianceStatus.Completed),
                    HasCompletedCounseling = v.CounselingRecords.Any(s => s.Status == ComplianceStatus.Completed),
                    CaseDate = v.FiledCase != null ? v.FiledCase.DateFiled : (DateOnly?)null
                })
                .ToListAsync();

            static string Fmt(DateOnly? d) => d.HasValue ? d.Value.ToString("MMMM d, yyyy") : string.Empty;

            var sections = slice
                .GroupBy(x => new { x.BarangayId, x.BarangayName })
                .OrderBy(g => g.Key.BarangayName)
                .Select(g =>
                {
                    var items = g.OrderBy(x => x.DateOfOccurrence).ThenBy(x => x.OvrNumber).ToList();

                    static bool Complied(ViolationStatus s, bool seminar, bool cs, bool counseling) =>
                        s is ViolationStatus.Paid or ViolationStatus.Closed || seminar || cs || counseling;

                    var rows = items.Select(x =>
                    {
                        string remarks;
                        if (x.PaymentDate.HasValue)
                            remarks = $"Complied and paid penalty {Fmt(x.PaymentDate)}";
                        else if (x.CompletedSeminar != null)
                            remarks = $"{(x.CompletedSeminar.SeminarType == SeminarType.VapingCessation ? "Vaping" : x.CompletedSeminar.SeminarType == SeminarType.SmokingCessation ? "Smoking" : "Solid waste")} seminar completed {Fmt(x.CompletedSeminar.DateCompleted)}".Trim();
                        else if (x.HasCompletedCommunityService)
                            remarks = $"Community service completed {Fmt(x.CompletedCommunityService)}".Trim();
                        else if (x.HasCompletedCounseling)
                            remarks = $"Counseling completed {Fmt(x.CompletedCounseling)}".Trim();
                        else if (x.Status == ViolationStatus.CaseFiled)
                            remarks = $"Case filed {Fmt(x.CaseDate)}".Trim();
                        else
                            remarks = x.Remarks ?? string.Empty;

                        return new SeminarViolationRowDTO
                        {
                            Date = x.DateOfOccurrence,
                            Division = x.Division.ToString(),
                            Gender = x.Gender.ToString(),
                            Age = x.AgeAtApprehension,
                            OvrNumber = x.OvrNumber,
                            Place = x.PlaceOfOccurrence ?? g.Key.BarangayName,
                            OrdinanceCode = OrdinanceCode(x.OrdinanceNumber, x.Series, x.IsExecutiveOrder).Replace("ORDINANCE NO.", "ORD.")
                                          + (x.SectionCode != null ? $" {x.SectionCode}" : string.Empty),
                            OrdinanceName = x.OrdinanceShortName,
                            Remarks = remarks
                        };
                    }).ToList();

                    var enforcers = items
                        .Where(x => !string.IsNullOrWhiteSpace(x.ApprehendingOfficer))
                        .GroupBy(x => x.ApprehendingOfficer!.Trim().ToUpperInvariant())
                        .Select(e => new SeminarEnforcerRowDTO
                        {
                            Name = e.First().ApprehendingOfficer!.Trim(),
                            DeputationNumber = e.Select(x => x.ApprehendingOfficerDeputationNumber).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d)) ?? string.Empty,
                            Apprehensions = e.Count()
                        })
                        .OrderBy(e => e.Name)
                        .ToList();

                    return new SeminarBarangaySectionDTO
                    {
                        BarangayId = g.Key.BarangayId.ToString(),
                        BarangayName = g.Key.BarangayName,
                        Recorded = items.Count,
                        Complied = items.Count(x => Complied(x.Status, x.CompletedSeminar != null, x.HasCompletedCommunityService, x.HasCompletedCounseling)),
                        CaseFiled = items.Count(x => x.Status == ViolationStatus.CaseFiled
                            && !Complied(x.Status, x.CompletedSeminar != null, x.HasCompletedCommunityService, x.HasCompletedCounseling)),
                        Dismissed = items.Count(x => x.Status == ViolationStatus.Dismissed),
                        NotComplied = items.Count(x => x.Status != ViolationStatus.CaseFiled && x.Status != ViolationStatus.Dismissed
                            && !Complied(x.Status, x.CompletedSeminar != null, x.HasCompletedCommunityService, x.HasCompletedCounseling)),
                        Divisions = items.Select(x => x.Division.ToString()).Distinct().OrderBy(d => d).ToList(),
                        Rows = rows,
                        Enforcers = enforcers
                    };
                })
                .ToList();

            return new SeminarBarangayReportDTO { From = from, To = to, Barangays = sections };
        }

        public async Task<IncentiveReportDTO> GetIncentivesAsync(DateOnly from, DateOnly to, Division? division)
        {
            var query = _db.Payments.AsNoTracking()
                .Where(p => p.DateOfPayment >= from && p.DateOfPayment <= to && !p.Violation.IsWarningTicket);
            if (division.HasValue)
                query = query.Where(p => p.Violation.Division == division);

            var payments = await query
                .Select(p => new
                {
                    p.DateOfPayment,
                    p.Violation.Division,
                    p.Violation.ApprehendingOfficer,
                    p.Amount
                })
                .ToListAsync();

            var divisions = payments
                .GroupBy(p => p.Division)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var rows = g
                        .Select(p => new IncentivePaymentRowDTO
                        {
                            DateOfPayment = p.DateOfPayment,
                            Division = g.Key.ToString(),
                            Deputized = string.IsNullOrWhiteSpace(p.ApprehendingOfficer) ? "UNSPECIFIED" : p.ApprehendingOfficer.Trim().ToUpperInvariant(),
                            Amount = p.Amount,
                            Incentive = Math.Round(p.Amount * IncentiveRate, 2)
                        })
                        .OrderBy(r => r.Deputized).ThenBy(r => r.DateOfPayment)
                        .ToList();

                    return new IncentiveDivisionDTO
                    {
                        Division = g.Key.ToString(),
                        Payments = rows,
                        EnforcerTotals = rows
                            .GroupBy(r => r.Deputized)
                            .Select(e => new IncentiveEnforcerTotalDTO { Deputized = e.Key, TotalIncentive = e.Sum(r => r.Incentive) })
                            .OrderBy(e => e.Deputized)
                            .ToList(),
                        TotalAmount = rows.Sum(r => r.Amount),
                        TotalIncentive = rows.Sum(r => r.Incentive)
                    };
                })
                .ToList();

            return new IncentiveReportDTO { From = from, To = to, Rate = IncentiveRate, Divisions = divisions };
        }

        public async Task<EnforcerListDTO> GetEnforcersAsync()
        {
            var officers = await _db.Users.AsNoTracking()
                .Where(u => u.Role == Role.ApprehendingOfficer && u.IsActive)
                .Select(u => new { u.FullName, u.Username, u.DeputationNumber })
                .ToListAsync();

            // Office isn't stored on the user; take it from the unit they apprehend under.
            var usage = await _db.Violations.AsNoTracking()
                .Where(v => v.ApprehendingOfficer != null || v.ApprehendingOfficerDeputationNumber != null)
                .Select(v => new { v.ApprehendingOfficer, v.ApprehendingOfficerDeputationNumber, v.Division, v.DateOfOccurrence })
                .ToListAsync();

            string Office(string? name, string? deputation)
            {
                var match = usage
                    .Where(v => (!string.IsNullOrWhiteSpace(deputation) && v.ApprehendingOfficerDeputationNumber == deputation)
                             || (!string.IsNullOrWhiteSpace(name) && string.Equals(v.ApprehendingOfficer?.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)))
                    .OrderByDescending(v => v.DateOfOccurrence)
                    .FirstOrDefault();
                return match?.Division.ToString() ?? string.Empty;
            }

            var rows = officers
                .Select(o =>
                {
                    var name = string.IsNullOrWhiteSpace(o.FullName) ? o.Username : o.FullName!;
                    return new EnforcerRowDTO { Office = Office(name, o.DeputationNumber), Name = name, DeputationNumber = o.DeputationNumber ?? string.Empty };
                })
                .ToList();

            // Officers who only appear on paper tickets (not registered users).
            var known = new HashSet<string>(rows.Select(r => r.Name.Trim().ToUpperInvariant()));
            rows.AddRange(usage
                .Where(v => !string.IsNullOrWhiteSpace(v.ApprehendingOfficer))
                .GroupBy(v => v.ApprehendingOfficer!.Trim().ToUpperInvariant())
                .Where(g => !known.Contains(g.Key))
                .Select(g => new EnforcerRowDTO
                {
                    Office = g.OrderByDescending(v => v.DateOfOccurrence).First().Division.ToString(),
                    Name = g.First().ApprehendingOfficer!.Trim(),
                    DeputationNumber = g.Select(v => v.ApprehendingOfficerDeputationNumber).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d)) ?? string.Empty
                }));

            return new EnforcerListDTO
            {
                Rows = rows.OrderBy(r => r.Office == string.Empty ? 1 : 0).ThenBy(r => r.Office).ThenBy(r => r.Name).ToList()
            };
        }
    }
}
