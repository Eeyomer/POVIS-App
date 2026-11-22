using Microsoft.EntityFrameworkCore;
using POVISAPI.Common;
using POVISAPI.Data;
using POVISAPI.Interfaces;
using POVISAPI.Models.DTO;
using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Services
{
    /// <summary>
    /// Incentives earned by apprehending officers. Each paid (non-warning) OVR carries the incentive rate and amount
    /// that were frozen on the Payment when it was recorded, so this only reads them and never recomputes.
    /// </summary>
    public class IncentiveService : IIncentiveService
    {
        private readonly AppDbContext db;
        public IncentiveService(AppDbContext db) => this.db = db;

        public static (DateOnly From, DateOnly To) ResolveRange(IncentiveFilterDTO f)
        {
            var anchor = f.Date ?? Clock.PhToday;
            switch ((f.Period ?? "monthly").Trim().ToLowerInvariant())
            {
                case "daily":
                    return (anchor, anchor);
                case "weekly":
                    var offset = ((int)anchor.DayOfWeek + 6) % 7; // Monday = 0
                    var monday = anchor.AddDays(-offset);
                    return (monday, monday.AddDays(6));
                case "monthly":
                    var first = new DateOnly(anchor.Year, anchor.Month, 1);
                    return (first, first.AddMonths(1).AddDays(-1));
                case "annual":
                    return (new DateOnly(anchor.Year, 1, 1), new DateOnly(anchor.Year, 12, 31));
                case "custom":
                    if (f.From is null || f.To is null)
                        throw new ArgumentException("A custom period needs both From and To dates.");
                    if (f.From > f.To)
                        throw new ArgumentException("From date cannot be after To date.");
                    return (f.From.Value, f.To.Value);
                default:
                    throw new ArgumentException("Period must be daily, weekly, monthly, annual or custom.");
            }
        }

        public async Task<IncentiveListDTO> GetAsync(IncentiveFilterDTO f, Guid? forcedOfficerId)
        {
            var (from, to) = ResolveRange(f);
            var officerId = forcedOfficerId ?? f.OfficerId;

            var query = db.Payments.AsNoTracking()
                .Where(p => p.DateOfPayment >= from && p.DateOfPayment <= to
                    && !p.Violation.IsWarningTicket
                    && (p.Violation.ApprehendingOfficerId != null || p.Violation.ApprehendingOfficer != null));

            if (officerId.HasValue)
                query = query.Where(p => p.Violation.ApprehendingOfficerId == officerId);

            if (f.Division.HasValue)
                query = query.Where(p => p.Violation.Division == f.Division);

            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                var term = $"%{f.Search.Trim()}%";
                query = query.Where(p =>
                    EF.Functions.ILike(p.Violation.OvrNumber, term) ||
                    EF.Functions.ILike(p.Violation.Violator.LastName, term) ||
                    EF.Functions.ILike(p.Violation.Violator.FirstName, term) ||
                    (p.ReceiptNumber != null && EF.Functions.ILike(p.ReceiptNumber, term)));
            }

            var totalCount = await query.CountAsync();
            var totalAmount = await query.SumAsync(p => (decimal?)p.Amount) ?? 0m;
            var totalIncentive = await query.SumAsync(p => (decimal?)p.IncentiveAmount) ?? 0m;

            var officerGroups = await query
                .GroupBy(p => new
                {
                    p.Violation.ApprehendingOfficerId,
                    Name = p.Violation.ApprehendingOfficerUser != null
                        ? (p.Violation.ApprehendingOfficerUser.FullName ?? p.Violation.ApprehendingOfficerUser.Username)
                        : p.Violation.ApprehendingOfficer
                })
                .Select(g => new
                {
                    g.Key.ApprehendingOfficerId,
                    g.Key.Name,
                    Count = g.Count(),
                    Amount = g.Sum(p => p.Amount),
                    Incentive = g.Sum(p => p.IncentiveAmount)
                })
                .ToListAsync();

            var officerTotals = officerGroups
                .Select(g => new IncentiveOfficerTotalDTO
                {
                    OfficerId = g.ApprehendingOfficerId?.ToString(),
                    OfficerName = g.Name ?? "Unspecified",
                    Count = g.Count,
                    TotalAmount = g.Amount,
                    TotalIncentive = g.Incentive
                })
                .OrderByDescending(o => o.TotalIncentive).ThenBy(o => o.OfficerName)
                .ToList();

            var rows = await query
                .OrderByDescending(p => p.DateOfPayment).ThenBy(p => p.Violation.OvrNumber)
                .Skip((f.Page - 1) * f.PageSize).Take(f.PageSize)
                .Select(p => new
                {
                    p.Id,
                    p.ViolationId,
                    p.DateOfPayment,
                    p.ReceiptNumber,
                    p.Amount,
                    p.IncentiveRate,
                    p.IncentiveAmount,
                    p.Violation.ApprehendingOfficerId,
                    OfficerName = p.Violation.ApprehendingOfficerUser != null
                        ? (p.Violation.ApprehendingOfficerUser.FullName ?? p.Violation.ApprehendingOfficerUser.Username)
                        : p.Violation.ApprehendingOfficer,
                    p.Violation.Division,
                    p.Violation.OvrNumber,
                    ViolatorLast = p.Violation.Violator.LastName,
                    ViolatorFirst = p.Violation.Violator.FirstName,
                    p.Violation.DateOfOccurrence,
                    p.Violation.PlaceOfOccurrence,
                    OrdinanceNumber = p.Violation.Ordinance.OrdinanceNumber,
                    OrdinanceSeries = p.Violation.Ordinance.Series,
                    OrdinanceName = p.Violation.Ordinance.ShortName,
                    Section = p.Violation.OrdinanceSection != null ? p.Violation.OrdinanceSection.SectionCode : null,
                    p.Violation.OffenseLevel,
                    p.Violation.Status
                })
                .ToListAsync();

            return new IncentiveListDTO
            {
                Period = (f.Period ?? "monthly").Trim().ToLowerInvariant(),
                From = from,
                To = to,
                TotalCount = totalCount,
                TotalAmount = totalAmount,
                TotalIncentive = totalIncentive,
                Page = f.Page,
                PageSize = f.PageSize,
                OfficerTotals = officerTotals,
                Items = rows.Select(r => new IncentiveRowDTO
                {
                    PaymentId = r.Id.ToString(),
                    ViolationId = r.ViolationId.ToString(),
                    DateOfPayment = r.DateOfPayment,
                    ReceiptNumber = r.ReceiptNumber,
                    Amount = r.Amount,
                    IncentiveRate = r.IncentiveRate,
                    IncentiveAmount = r.IncentiveAmount,
                    OfficerId = r.ApprehendingOfficerId?.ToString(),
                    OfficerName = r.OfficerName ?? "Unspecified",
                    Division = r.Division.ToString(),
                    OvrNumber = r.OvrNumber,
                    ViolatorFullName = r.ViolatorLast + ", " + r.ViolatorFirst,
                    DateOfOccurrence = r.DateOfOccurrence,
                    PlaceOfOccurrence = r.PlaceOfOccurrence,
                    Ordinance = "Ord. " + r.OrdinanceNumber + " S." + r.OrdinanceSeries + " - " + r.OrdinanceName,
                    Section = r.Section,
                    OffenseLevel = OffenseLevels.Ordinal(r.OffenseLevel) + " offense",
                    Status = r.Status.ToString()
                }).ToList()
            };
        }
    }
}
