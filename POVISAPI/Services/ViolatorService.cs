using Microsoft.EntityFrameworkCore;
using POVISAPI.Data;
using POVISAPI.Interfaces;
using POVISAPI.Models.DTO;
using POVISAPI.Models.Entity;

namespace POVISAPI.Services
{
    public class ViolatorService : IViolatorService
    {
        private readonly AppDbContext dbContext;
        public ViolatorService(AppDbContext _dbContext) => dbContext = _dbContext;

        private static ViolatorDTO MapToDTO(Violator v) => new()
        {
            ViolatorId = v.Id.ToString(),
            LastName = v.LastName,
            FirstName = v.FirstName,
            MiddleName = v.MiddleName,
            Suffix = v.Suffix,
            Gender = v.Gender,
            Birthday = v.Birthday,
            Address = v.Address,
            BarangayId = v.BarangayId?.ToString(),
            BarangayName = v.Barangay?.Name,
            City = v.City,
            PresentedValidId = v.PresentedValidId,
            ContactNumber = v.ContactNumber,
            ViolationCount = v.Violations?.Count ?? 0
        };

        public async Task<PagedResult<ViolatorDTO>> SearchAsync(string? search, int page = 1, int pageSize = 20)
        {
            var query = dbContext.Violators
                .Include(v => v.Barangay)
                .Include(v => v.Violations)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(v =>
                    EF.Functions.ILike(v.LastName, $"%{term}%") ||
                    EF.Functions.ILike(v.FirstName, $"%{term}%") ||
                    (v.MiddleName != null && EF.Functions.ILike(v.MiddleName, $"%{term}%")));
            }

            var total = await query.CountAsync();
            var entities = await query
                .OrderBy(v => v.LastName).ThenBy(v => v.FirstName)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            return new PagedResult<ViolatorDTO>
            {
                Items = entities.Select(MapToDTO).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ViolatorDTO> GetByIdAsync(string id)
        {
            var entity = await GetTrackedViolator(id);
            return MapToDTO(entity);
        }

        public async Task<List<ViolatorHistoryDTO>> GetHistoryAsync(string id)
        {
            if (!Guid.TryParse(id, out Guid parsedId))
                throw new ArgumentException("Invalid Violator Id format.");

            var entities = await dbContext.Violations
                .Include(v => v.Ordinance)
                .Where(v => v.ViolatorId == parsedId)
                .OrderByDescending(v => v.DateOfOccurrence)
                .ToListAsync();

            return entities.Select(v => new ViolatorHistoryDTO
            {
                ViolationId = v.Id.ToString(),
                OvrNumber = v.OvrNumber,
                DateOfOccurrence = v.DateOfOccurrence,
                OrdinanceShortName = v.Ordinance.ShortName,
                OffenseLevel = v.OffenseLevel,
                Status = v.Status
            }).ToList();
        }

        public async Task<ViolatorDTO> CreateAsync(AddViolatorDTO dto)
        {
            Guid? barangayId = null;
            if (!string.IsNullOrWhiteSpace(dto.BarangayId))
            {
                if (!Guid.TryParse(dto.BarangayId, out Guid parsedBarangayId))
                    throw new ArgumentException("Invalid Barangay Id format.");
                barangayId = parsedBarangayId;
            }

            var entity = new Violator
            {
                LastName = dto.LastName.Trim(),
                FirstName = dto.FirstName.Trim(),
                MiddleName = dto.MiddleName?.Trim(),
                Suffix = dto.Suffix?.Trim(),
                Gender = dto.Gender,
                Birthday = dto.Birthday,
                Address = dto.Address?.Trim(),
                BarangayId = barangayId,
                City = dto.City?.Trim(),
                PresentedValidId = dto.PresentedValidId?.Trim(),
                ContactNumber = dto.ContactNumber?.Trim()
            };
            await dbContext.Violators.AddAsync(entity);
            await dbContext.SaveChangesAsync();
            return await GetByIdAsync(entity.Id.ToString());
        }

        public async Task<ViolatorDTO> UpdateAsync(UpdateViolatorDTO dto)
        {
            var entity = await GetTrackedViolator(dto.ViolatorId);

            Guid? barangayId = null;
            if (!string.IsNullOrWhiteSpace(dto.BarangayId))
            {
                if (!Guid.TryParse(dto.BarangayId, out Guid parsedBarangayId))
                    throw new ArgumentException("Invalid Barangay Id format.");
                barangayId = parsedBarangayId;
            }

            entity.LastName = dto.LastName.Trim();
            entity.FirstName = dto.FirstName.Trim();
            entity.MiddleName = dto.MiddleName?.Trim();
            entity.Suffix = dto.Suffix?.Trim();
            entity.Gender = dto.Gender;
            entity.Birthday = dto.Birthday;
            entity.Address = dto.Address?.Trim();
            entity.BarangayId = barangayId;
            entity.City = dto.City?.Trim();
            entity.PresentedValidId = dto.PresentedValidId?.Trim();
            entity.ContactNumber = dto.ContactNumber?.Trim();
            await dbContext.SaveChangesAsync();
            return await GetByIdAsync(entity.Id.ToString());
        }

        public async Task DeleteAsync(string id)
        {
            var entity = await GetTrackedViolator(id);

            var hasViolations = await dbContext.Violations.AnyAsync(v => v.ViolatorId == entity.Id);
            if (hasViolations)
                throw new InvalidOperationException("Violator cannot be deleted because they have violation history.");

            dbContext.Violators.Remove(entity);
            await dbContext.SaveChangesAsync();
        }

        private async Task<Violator> GetTrackedViolator(string id)
        {
            if (!Guid.TryParse(id, out Guid parsedId))
                throw new ArgumentException("Invalid Violator Id format.");

            var entity = await dbContext.Violators
                .Include(v => v.Barangay)
                .Include(v => v.Violations)
                .FirstOrDefaultAsync(v => v.Id == parsedId);
            if (entity == null)
                throw new KeyNotFoundException("Violator does not exist.");

            return entity;
        }
    }
}
