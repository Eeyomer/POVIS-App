using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using POVISAPI.Common;
using POVISAPI.Data;
using POVISAPI.Interfaces;
using POVISAPI.Models.DTO;
using POVISAPI.Models.Entity;
using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Services
{
    public class OrdinanceService : IOrdinanceService
    {
        private readonly AppDbContext dbContext;
        private readonly IMemoryCache cache;
        public OrdinanceService(AppDbContext _dbContext, IMemoryCache _cache)
        {
            dbContext = _dbContext;
            cache = _cache;
        }

        /// <summary>Penalty ladders are cached per ordinance; drop the cached copy whenever one is added or removed.</summary>
        private void InvalidatePenalties(Guid ordinanceId) => cache.Remove(CacheKeys.Penalties(ordinanceId));

        private static OrdinancePenaltyDTO MapPenalty(OrdinancePenalty p) => new()
        {
            OrdinancePenaltyId = p.Id.ToString(),
            OrdinanceSectionId = p.OrdinanceSectionId?.ToString(),
            OffenseLevel = p.OffenseLevel,
            OffenderType = p.OffenderType,
            FineAmount = p.FineAmount,
            SeminarDays = p.SeminarDays,
            CommunityServiceHours = p.CommunityServiceHours,
            CommunityServiceDays = p.CommunityServiceDays,
            RequiresCounseling = p.RequiresCounseling,
            HasImprisonment = p.HasImprisonment,
            ImprisonmentTerm = p.ImprisonmentTerm,
            BusinessPermitAction = p.BusinessPermitAction,
            IsConjunctive = p.IsConjunctive,
            PenaltyText = p.PenaltyText
        };

        private static OrdinanceDTO MapToDTO(Ordinance o) => new()
        {
            OrdinanceId = o.Id.ToString(),
            OrdinanceNumber = o.OrdinanceNumber,
            Series = o.Series,
            Title = o.Title,
            ShortName = o.ShortName,
            IsExecutiveOrder = o.IsExecutiveOrder,
            IsActive = o.IsActive,
            Sections = o.Sections
                .OrderBy(s => s.SectionCode)
                .Select(s => new OrdinanceSectionDTO
                {
                    OrdinanceSectionId = s.Id.ToString(),
                    SectionCode = s.SectionCode,
                    Description = s.Description,
                    Penalties = s.Penalties.OrderBy(p => p.OffenseLevel).Select(MapPenalty).ToList()
                })
                .ToList(),
            Penalties = o.Penalties
                .Where(p => p.OrdinanceSectionId == null)
                .OrderBy(p => p.OffenseLevel)
                .Select(MapPenalty)
                .ToList()
        };

        public async Task<List<OrdinanceListDTO>> GetAllAsync(bool includeInactive = false)
        {
            var query = dbContext.Ordinances.AsNoTracking().AsQueryable();
            if (!includeInactive) query = query.Where(o => o.IsActive);

            var entities = await query.OrderByDescending(o => o.Series).ThenBy(o => o.OrdinanceNumber).ToListAsync();
            return entities.Select(o => new OrdinanceListDTO
            {
                OrdinanceId = o.Id.ToString(),
                OrdinanceNumber = o.OrdinanceNumber,
                Series = o.Series,
                ShortName = o.ShortName,
                IsActive = o.IsActive
            }).ToList();
        }

        public async Task<OrdinanceDTO> GetByIdAsync(string id)
        {
            var entity = await GetTrackedOrdinance(id, includeSections: true);
            return MapToDTO(entity);
        }

        public async Task<OrdinanceDTO> CreateAsync(AddOrdinanceDTO dto)
        {
            var entity = new Ordinance
            {
                OrdinanceNumber = dto.OrdinanceNumber.Trim(),
                Series = dto.Series,
                Title = dto.Title.Trim(),
                ShortName = dto.ShortName.Trim(),
                IsExecutiveOrder = dto.IsExecutiveOrder
            };
            await dbContext.Ordinances.AddAsync(entity);
            await dbContext.SaveChangesAsync();
            return MapToDTO(entity);
        }

        public async Task<OrdinanceDTO> UpdateAsync(UpdateOrdinanceDTO dto)
        {
            var entity = await GetTrackedOrdinance(dto.OrdinanceId, includeSections: true);

            entity.OrdinanceNumber = dto.OrdinanceNumber.Trim();
            entity.Series = dto.Series;
            entity.Title = dto.Title.Trim();
            entity.ShortName = dto.ShortName.Trim();
            entity.IsExecutiveOrder = dto.IsExecutiveOrder;
            entity.IsActive = dto.IsActive;
            await dbContext.SaveChangesAsync();
            return MapToDTO(entity);
        }

        public async Task DeleteAsync(string id)
        {
            var entity = await GetTrackedOrdinance(id, includeSections: false);

            var hasViolations = await dbContext.Violations.AnyAsync(v => v.OrdinanceId == entity.Id);
            if (hasViolations)
            {
                entity.IsActive = false;
            }
            else
            {
                dbContext.Ordinances.Remove(entity);
            }
            await dbContext.SaveChangesAsync();
            InvalidatePenalties(entity.Id);
        }

        public async Task<OrdinanceSectionDTO> AddSectionAsync(string ordinanceId, AddOrdinanceSectionDTO dto)
        {
            var ordinance = await GetTrackedOrdinance(ordinanceId, includeSections: false);

            var section = new OrdinanceSection
            {
                OrdinanceId = ordinance.Id,
                SectionCode = dto.SectionCode.Trim(),
                Description = dto.Description?.Trim()
            };
            await dbContext.OrdinanceSections.AddAsync(section);
            await dbContext.SaveChangesAsync();

            return new OrdinanceSectionDTO
            {
                OrdinanceSectionId = section.Id.ToString(),
                SectionCode = section.SectionCode,
                Description = section.Description
            };
        }

        public async Task RemoveSectionAsync(string ordinanceId, string sectionId)
        {
            if (!Guid.TryParse(ordinanceId, out Guid parsedOrdinanceId))
                throw new ArgumentException("Invalid Ordinance Id format.");
            if (!Guid.TryParse(sectionId, out Guid parsedSectionId))
                throw new ArgumentException("Invalid Section Id format.");

            var section = await dbContext.OrdinanceSections
                .FirstOrDefaultAsync(s => s.Id == parsedSectionId && s.OrdinanceId == parsedOrdinanceId);
            if (section == null)
                throw new KeyNotFoundException("Section does not exist.");

            var hasViolations = await dbContext.Violations.AnyAsync(v => v.OrdinanceSectionId == parsedSectionId);
            if (hasViolations)
                throw new InvalidOperationException("Section cannot be removed because it is referenced by existing violations.");

            dbContext.OrdinanceSections.Remove(section);
            await dbContext.SaveChangesAsync();
            InvalidatePenalties(parsedOrdinanceId); // a removed section takes its penalties with it
        }

        public async Task<OrdinancePenaltyDTO> AddPenaltyAsync(string ordinanceId, AddOrdinancePenaltyDTO dto)
        {
            var ordinance = await GetTrackedOrdinance(ordinanceId, includeSections: false);

            Guid? sectionId = null;
            if (!string.IsNullOrWhiteSpace(dto.OrdinanceSectionId))
            {
                if (!Guid.TryParse(dto.OrdinanceSectionId, out Guid parsedSectionId))
                    throw new ArgumentException("Invalid Ordinance Section Id format.");
                sectionId = parsedSectionId;
            }

            // Levels are sequential and escalating per section (or per whole ordinance when no section): a new level is
            // always the next one up, and it must not be a lighter fine than the level before it.
            var siblings = await dbContext.OrdinancePenalties.AsNoTracking()
                .Where(p => p.OrdinanceId == ordinance.Id && p.OrdinanceSectionId == sectionId && p.OffenderType == dto.OffenderType)
                .ToListAsync();
            var nextLevel = siblings.Count == 0 ? 1 : siblings.Max(p => p.OffenseLevel) + 1;
            var level = dto.OffenseLevel ?? nextLevel;
            if (siblings.Any(p => p.OffenseLevel == level))
                throw new ArgumentException($"A {OffenseLevels.Ordinal(level)} offense penalty already exists here. Add the next level ({OffenseLevels.Ordinal(nextLevel)}) instead.");
            if (level != nextLevel)
                throw new ArgumentException($"Levels must be added in order. The next level is the {OffenseLevels.Ordinal(nextLevel)} offense.");
            if (level > OffenseLevels.Max)
                throw new ArgumentException($"At most {OffenseLevels.Max} levels are allowed.");
            var previous = siblings.OrderByDescending(p => p.OffenseLevel).FirstOrDefault();
            if (previous?.FineAmount is decimal prevFine && dto.FineAmount is decimal newFine && newFine < prevFine)
                throw new ArgumentException($"The {OffenseLevels.Ordinal(level)} offense must be higher than the {OffenseLevels.Ordinal(previous.OffenseLevel)} offense (fine of ₱{prevFine:N2}). Enter a fine of at least that amount.");

            var penalty = new OrdinancePenalty
            {
                OrdinanceId = ordinance.Id,
                OrdinanceSectionId = sectionId,
                OffenseLevel = level,
                OffenderType = dto.OffenderType,
                FineAmount = dto.FineAmount,
                SeminarDays = dto.SeminarDays,
                CommunityServiceHours = dto.CommunityServiceHours,
                CommunityServiceDays = dto.CommunityServiceDays,
                RequiresCounseling = dto.RequiresCounseling,
                HasImprisonment = dto.HasImprisonment,
                ImprisonmentTerm = dto.ImprisonmentTerm?.Trim(),
                BusinessPermitAction = dto.BusinessPermitAction,
                IsConjunctive = dto.IsConjunctive,
                PenaltyText = dto.PenaltyText.Trim()
            };
            await dbContext.OrdinancePenalties.AddAsync(penalty);
            await dbContext.SaveChangesAsync();
            InvalidatePenalties(ordinance.Id);
            return MapPenalty(penalty);
        }

        public async Task RemovePenaltyAsync(string ordinanceId, string penaltyId)
        {
            if (!Guid.TryParse(ordinanceId, out Guid parsedOrdinanceId))
                throw new ArgumentException("Invalid Ordinance Id format.");
            if (!Guid.TryParse(penaltyId, out Guid parsedPenaltyId))
                throw new ArgumentException("Invalid Penalty Id format.");

            var penalty = await dbContext.OrdinancePenalties
                .FirstOrDefaultAsync(p => p.Id == parsedPenaltyId && p.OrdinanceId == parsedOrdinanceId);
            if (penalty == null)
                throw new KeyNotFoundException("Penalty does not exist.");

            // Keep the ladder gap-free: only the highest level of a section can be removed.
            var hasHigher = await dbContext.OrdinancePenalties.AnyAsync(p =>
                p.OrdinanceId == penalty.OrdinanceId && p.OrdinanceSectionId == penalty.OrdinanceSectionId
                && p.OffenderType == penalty.OffenderType && p.OffenseLevel > penalty.OffenseLevel);
            if (hasHigher)
                throw new InvalidOperationException("Only the highest level can be removed. Remove the higher offense levels first.");

            dbContext.OrdinancePenalties.Remove(penalty);
            await dbContext.SaveChangesAsync();
            InvalidatePenalties(parsedOrdinanceId);
        }

        private async Task<Ordinance> GetTrackedOrdinance(string id, bool includeSections)
        {
            if (!Guid.TryParse(id, out Guid parsedId))
                throw new ArgumentException("Invalid Ordinance Id format.");

            var query = dbContext.Ordinances.Include(o => o.Penalties).AsQueryable();
            if (includeSections)
                query = query.Include(o => o.Sections).ThenInclude(s => s.Penalties);

            var entity = await query.FirstOrDefaultAsync(o => o.Id == parsedId);
            if (entity == null)
                throw new KeyNotFoundException("Ordinance does not exist.");

            return entity;
        }
    }
}
