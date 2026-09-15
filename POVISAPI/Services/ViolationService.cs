using Microsoft.EntityFrameworkCore;
using POVISAPI.Data;
using POVISAPI.Interfaces;
using POVISAPI.Models.DTO;
using POVISAPI.Models.Entity;
using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Services
{
    public class ViolationService : IViolationService
    {
        private readonly AppDbContext dbContext;
        public ViolationService(AppDbContext _dbContext) => dbContext = _dbContext;

        private static ViolationDTO MapToDTO(Violation v) => new()
        {
            ViolationId = v.Id.ToString(),
            ViolatorId = v.ViolatorId.ToString(),
            ViolatorFullName = v.Violator.LastName + ", " + v.Violator.FirstName +
                (v.Violator.MiddleName != null ? " " + v.Violator.MiddleName : ""),
            OvrNumber = v.OvrNumber,
            DateOfSubmission = v.DateOfSubmission,
            Division = v.Division,
            AgeAtApprehension = v.AgeAtApprehension,
            DateOfOccurrence = v.DateOfOccurrence,
            TimeOfOccurrence = v.TimeOfOccurrence,
            PlaceOfOccurrence = v.PlaceOfOccurrence,
            BarangayOfOccurrenceId = v.BarangayOfOccurrenceId?.ToString(),
            BarangayOfOccurrenceName = v.BarangayOfOccurrence?.Name,
            Witness = v.Witness,
            ApprehendingOfficer = v.ApprehendingOfficer,
            OrdinanceId = v.OrdinanceId.ToString(),
            OrdinanceShortName = v.Ordinance.ShortName,
            OrdinanceSectionId = v.OrdinanceSectionId?.ToString(),
            OrdinanceSectionCode = v.OrdinanceSection?.SectionCode,
            OffenseLevel = v.OffenseLevel,
            IsWarningTicket = v.IsWarningTicket,
            Status = v.Status,
            Remarks = v.Remarks,
            Remarks2 = v.Remarks2,
            HasPayment = v.Payment != null,
            HasFiledCase = v.FiledCase != null,
            IsReviewed = v.IsReviewed,
            ReviewedAt = v.ReviewedAt,
            ReviewedBy = v.ReviewedBy
        };

        private IQueryable<Violation> IncludedQuery() =>
            dbContext.Violations
                .Include(v => v.Violator)
                .Include(v => v.Ordinance)
                .Include(v => v.OrdinanceSection)
                .Include(v => v.BarangayOfOccurrence)
                .Include(v => v.Payment)
                .Include(v => v.FiledCase);

        public async Task<PagedResult<ViolationDTO>> GetAllAsync(ViolationFilterDTO filter)
        {
            var query = IncludedQuery().AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.OrdinanceId))
            {
                if (!Guid.TryParse(filter.OrdinanceId, out Guid ordinanceId))
                    throw new ArgumentException("Invalid Ordinance Id format.");
                query = query.Where(v => v.OrdinanceId == ordinanceId);
            }

            if (!string.IsNullOrWhiteSpace(filter.BarangayId))
            {
                if (!Guid.TryParse(filter.BarangayId, out Guid barangayId))
                    throw new ArgumentException("Invalid Barangay Id format.");
                query = query.Where(v => v.BarangayOfOccurrenceId == barangayId);
            }

            if (filter.Status.HasValue)
                query = query.Where(v => v.Status == filter.Status);
            if (filter.DateFrom.HasValue)
                query = query.Where(v => v.DateOfOccurrence >= filter.DateFrom);
            if (filter.DateTo.HasValue)
                query = query.Where(v => v.DateOfOccurrence <= filter.DateTo);
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim();
                query = query.Where(v =>
                    EF.Functions.ILike(v.OvrNumber, $"%{term}%") ||
                    EF.Functions.ILike(v.Violator.LastName, $"%{term}%") ||
                    EF.Functions.ILike(v.Violator.FirstName, $"%{term}%"));
            }

            var total = await query.CountAsync();
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 100);

            var entities = await query
                .OrderByDescending(v => v.DateOfOccurrence).ThenBy(v => v.OvrNumber)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            return new PagedResult<ViolationDTO>
            {
                Items = entities.Select(MapToDTO).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ViolationDTO> GetByIdAsync(string id)
        {
            var entity = await GetTrackedViolation(id);
            return MapToDTO(entity);
        }

        public async Task<ViolationDTO> CreateAsync(AddViolationDTO dto)
        {
            if (!Guid.TryParse(dto.ViolatorId, out Guid violatorId))
                throw new ArgumentException("Invalid Violator Id format.");
            if (!Guid.TryParse(dto.OrdinanceId, out Guid ordinanceId))
                throw new ArgumentException("Invalid Ordinance Id format.");

            Guid? ordinanceSectionId = null;
            if (!string.IsNullOrWhiteSpace(dto.OrdinanceSectionId))
            {
                if (!Guid.TryParse(dto.OrdinanceSectionId, out Guid parsedSectionId))
                    throw new ArgumentException("Invalid Ordinance Section Id format.");
                ordinanceSectionId = parsedSectionId;
            }

            Guid? barangayOfOccurrenceId = null;
            if (!string.IsNullOrWhiteSpace(dto.BarangayOfOccurrenceId))
            {
                if (!Guid.TryParse(dto.BarangayOfOccurrenceId, out Guid parsedBarangayId))
                    throw new ArgumentException("Invalid Barangay Id format.");
                barangayOfOccurrenceId = parsedBarangayId;
            }

            bool violatorExists = await dbContext.Violators.AnyAsync(v => v.Id == violatorId);
            if (!violatorExists)
                throw new KeyNotFoundException("Violator does not exist.");

            bool ordinanceExists = await dbContext.Ordinances.AnyAsync(o => o.Id == ordinanceId && o.IsActive);
            if (!ordinanceExists)
                throw new KeyNotFoundException("Ordinance does not exist or is inactive.");

            // Snapshot the offense level from the violator's prior history against
            // the same ordinance (warning tickets don't count toward offense level).
            var priorCount = await dbContext.Violations.CountAsync(v =>
                v.ViolatorId == violatorId &&
                v.OrdinanceId == ordinanceId &&
                !v.IsWarningTicket &&
                v.Status != ViolationStatus.Dismissed);

            var offenseLevel = priorCount switch
            {
                0 => OffenseLevel.First,
                1 => OffenseLevel.Second,
                2 => OffenseLevel.Third,
                _ => OffenseLevel.Fourth
            };

            var entity = new Violation
            {
                ViolatorId = violatorId,
                OvrNumber = dto.OvrNumber.Trim(),
                DateOfSubmission = dto.DateOfSubmission,
                Division = dto.Division,
                AgeAtApprehension = dto.AgeAtApprehension,
                DateOfOccurrence = dto.DateOfOccurrence,
                TimeOfOccurrence = dto.TimeOfOccurrence,
                PlaceOfOccurrence = dto.PlaceOfOccurrence?.Trim(),
                BarangayOfOccurrenceId = barangayOfOccurrenceId,
                Witness = dto.Witness?.Trim(),
                ApprehendingOfficer = dto.ApprehendingOfficer?.Trim(),
                OrdinanceId = ordinanceId,
                OrdinanceSectionId = ordinanceSectionId,
                OffenseLevel = offenseLevel,
                IsWarningTicket = dto.IsWarningTicket,
                Status = dto.IsWarningTicket ? ViolationStatus.WarningIssued : ViolationStatus.Pending,
                Remarks = dto.Remarks?.Trim(),
                Remarks2 = dto.Remarks2?.Trim()
            };
            await dbContext.Violations.AddAsync(entity);
            await dbContext.SaveChangesAsync();
            return await GetByIdAsync(entity.Id.ToString());
        }

        public async Task<ViolationDTO> UpdateAsync(UpdateViolationDTO dto)
        {
            var entity = await GetTrackedViolation(dto.ViolationId);

            Guid? ordinanceSectionId = null;
            if (!string.IsNullOrWhiteSpace(dto.OrdinanceSectionId))
            {
                if (!Guid.TryParse(dto.OrdinanceSectionId, out Guid parsedSectionId))
                    throw new ArgumentException("Invalid Ordinance Section Id format.");
                ordinanceSectionId = parsedSectionId;
            }

            Guid? barangayOfOccurrenceId = null;
            if (!string.IsNullOrWhiteSpace(dto.BarangayOfOccurrenceId))
            {
                if (!Guid.TryParse(dto.BarangayOfOccurrenceId, out Guid parsedBarangayId))
                    throw new ArgumentException("Invalid Barangay Id format.");
                barangayOfOccurrenceId = parsedBarangayId;
            }

            entity.DateOfSubmission = dto.DateOfSubmission;
            entity.Division = dto.Division;
            entity.AgeAtApprehension = dto.AgeAtApprehension;
            entity.DateOfOccurrence = dto.DateOfOccurrence;
            entity.TimeOfOccurrence = dto.TimeOfOccurrence;
            entity.PlaceOfOccurrence = dto.PlaceOfOccurrence?.Trim();
            entity.BarangayOfOccurrenceId = barangayOfOccurrenceId;
            entity.Witness = dto.Witness?.Trim();
            entity.ApprehendingOfficer = dto.ApprehendingOfficer?.Trim();
            entity.OrdinanceSectionId = ordinanceSectionId;
            entity.Status = dto.Status;
            entity.Remarks = dto.Remarks?.Trim();
            entity.Remarks2 = dto.Remarks2?.Trim();
            // OffenseLevel and OrdinanceId are intentionally NOT updatable —
            // they were snapshotted at ticketing time (same rule as locked cost fields).
            await dbContext.SaveChangesAsync();
            return await GetByIdAsync(entity.Id.ToString());
        }

        public async Task DeleteAsync(string id)
        {
            var entity = await GetTrackedViolation(id);
            dbContext.Violations.Remove(entity);
            await dbContext.SaveChangesAsync();
        }

        public async Task<ViolationDTO> SetReviewedAsync(string id, bool reviewed, string? reviewerUsername)
        {
            var entity = await GetTrackedViolation(id);
            entity.IsReviewed = reviewed;
            entity.ReviewedAt = reviewed ? DateTime.UtcNow : null;
            entity.ReviewedBy = reviewed ? reviewerUsername : null;
            await dbContext.SaveChangesAsync();
            return await GetByIdAsync(entity.Id.ToString());
        }

        public async Task<ApplicablePenaltyDTO> GetApplicablePenaltyAsync(string id)
        {
            var violation = await GetTrackedViolation(id);

            var penalty = await dbContext.OrdinancePenalties.AsNoTracking()
                .Where(p => p.OrdinanceId == violation.OrdinanceId
                         && p.OffenseLevel == violation.OffenseLevel
                         && (p.OrdinanceSectionId == violation.OrdinanceSectionId
                             || p.OrdinanceSectionId == null))
                .OrderByDescending(p => p.OrdinanceSectionId != null)
                .FirstOrDefaultAsync();

            if (penalty == null)
                throw new KeyNotFoundException("No penalty schedule found for this violation.");

            return new ApplicablePenaltyDTO
            {
                OffenseLevel = penalty.OffenseLevel,
                FineAmount = penalty.FineAmount,
                PenaltyText = penalty.PenaltyText
            };
        }

        private async Task<Violation> GetTrackedViolation(string id)
        {
            if (!Guid.TryParse(id, out Guid parsedId))
                throw new ArgumentException("Invalid Violation Id format.");

            var entity = await IncludedQuery().FirstOrDefaultAsync(v => v.Id == parsedId);
            if (entity == null)
                throw new KeyNotFoundException("Violation does not exist.");

            return entity;
        }
    }
}
