using Microsoft.EntityFrameworkCore;
using POVISAPI.Data;
using POVISAPI.Models.DTO;
using POVISAPI.Models.Entity.Enums;
using POVISAPI.Interfaces;

namespace POVISAPI.Services
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _db;
        public ReportService(AppDbContext db) => _db = db;

        public async Task<OrdinanceSummaryReportDTO> GetOrdinanceSummaryAsync(DateOnly from, DateOnly to)
        {
            // Pull the slice once, aggregate in memory — row counts here
            // (thousands per half-year) are well within reason for this.
            var slice = await _db.Violations.AsNoTracking()
                .Where(v => v.DateOfOccurrence >= from && v.DateOfOccurrence <= to && !v.IsWarningTicket)
                .Select(v => new
                {
                    v.OrdinanceId,
                    OrdinanceShortName = v.Ordinance.ShortName,
                    v.Violator.Gender,
                    v.Status,
                    PaidAmount = v.Payment != null ? v.Payment.Amount : (decimal?)null,
                    HasSeminar = v.SeminarRecords.Any(),
                    HasCounseling = v.CounselingRecords.Any(),
                    HasCommunityService = v.CommunityServiceRecords.Any(),
                    // "unpaid" per the SEMI sheet = fine amount of unsettled violations
                    FineAmount = _db.OrdinancePenalties
                        .Where(p => p.OrdinanceId == v.OrdinanceId
                                 && p.OffenseLevel == v.OffenseLevel
                                 && (p.OrdinanceSectionId == v.OrdinanceSectionId || p.OrdinanceSectionId == null))
                        .OrderByDescending(p => p.OrdinanceSectionId != null)
                        .Select(p => p.FineAmount)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var rows = slice
                .GroupBy(x => new { x.OrdinanceId, x.OrdinanceShortName })
                .Select(g => new OrdinanceSummaryRowDTO
                {
                    OrdinanceId = g.Key.OrdinanceId.ToString(),
                    OrdinanceShortName = g.Key.OrdinanceShortName,
                    FemaleCount = g.Count(x => x.Gender == Gender.Female),
                    MaleCount = g.Count(x => x.Gender == Gender.Male),
                    TotalViolators = g.Count(),
                    SeminarCount = g.Count(x => x.HasSeminar),
                    CounselingCount = g.Count(x => x.HasCounseling),
                    CommunityServiceCount = g.Count(x => x.HasCommunityService),
                    DidNotComplyCount = g.Count(x => x.Status == ViolationStatus.DidNotComply || x.Status == ViolationStatus.Pending),
                    PaidCount = g.Count(x => x.PaidAmount.HasValue),
                    TotalAmountPaid = g.Sum(x => x.PaidAmount ?? 0m),
                    TotalAmountUnpaid = g.Where(x => !x.PaidAmount.HasValue
                                                  && x.Status != ViolationStatus.Dismissed
                                                  && x.Status != ViolationStatus.Closed)
                                         .Sum(x => x.FineAmount ?? 0m)
                })
                .OrderByDescending(r => r.TotalViolators)
                .ToList();

            return new OrdinanceSummaryReportDTO
            {
                From = from,
                To = to,
                Rows = rows,
                TotalFemale = rows.Sum(r => r.FemaleCount),
                TotalMale = rows.Sum(r => r.MaleCount),
                TotalViolators = rows.Sum(r => r.TotalViolators),
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

            var rows = counts
                .GroupBy(c => new { c.BarangayId, c.BarangayName, c.District })
                .Select(g => new BarangayOrdinanceRowDTO
                {
                    BarangayId = g.Key.BarangayId.ToString(),
                    BarangayName = g.Key.BarangayName,
                    District = g.Key.District,
                    CountsByOrdinance = g.ToDictionary(x => x.OrdinanceName, x => x.Count),
                    Total = g.Sum(x => x.Count)
                })
                .OrderBy(r => r.District).ThenBy(r => r.BarangayName)
                .ToList();

            return new PerBarangayReportDTO
            {
                From = from,
                To = to,
                OrdinanceColumns = columns,
                Rows = rows
            };
        }
    }
}
