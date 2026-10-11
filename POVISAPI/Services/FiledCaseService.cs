using Microsoft.EntityFrameworkCore;
using POVISAPI.Data;
using POVISAPI.Interfaces;
using POVISAPI.Models.DTO;
using POVISAPI.Models.Entity;
using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Services
{
    public class FiledCaseService : IFiledCaseService
    {
        private readonly AppDbContext dbContext;
        public FiledCaseService(AppDbContext _dbContext) => dbContext = _dbContext;

        private static FiledCaseDTO MapToDTO(FiledCase f) => new()
        {
            FiledCaseId = f.Id.ToString(),
            ViolationId = f.ViolationId.ToString(),
            OvrNumber = f.Violation.OvrNumber,
            ViolatorFullName = f.Violation.Violator.LastName + ", " + f.Violation.Violator.FirstName,
            DateFiled = f.DateFiled,
            NpsDocketNumber = f.NpsDocketNumber,
            Status = f.Status,
            DateResolved = f.DateResolved,
            ResolutionRemarks = f.ResolutionRemarks,
            Remarks = f.Remarks
        };

        private IQueryable<FiledCase> IncludedQuery() =>
            dbContext.FiledCases.Include(f => f.Violation).ThenInclude(v => v.Violator);

        public async Task<PagedResult<FiledCaseDTO>> GetAllAsync(CaseStatus? status, DateOnly? from, DateOnly? to, int page = 1, int pageSize = 20, string? search = null)
        {
            var query = IncludedQuery().AsNoTracking().AsQueryable();
            if (status.HasValue) query = query.Where(f => f.Status == status);
            if (from.HasValue) query = query.Where(f => f.DateFiled >= from);
            if (to.HasValue) query = query.Where(f => f.DateFiled <= to);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = $"%{search.Trim()}%";
                query = query.Where(x =>
                    EF.Functions.ILike(x.Violation.OvrNumber, term) ||
                    EF.Functions.ILike(x.Violation.Violator.LastName, term) ||
                    EF.Functions.ILike(x.Violation.Violator.FirstName, term) ||
                    (x.NpsDocketNumber != null && EF.Functions.ILike(x.NpsDocketNumber, term)));
            }

            var total = await query.CountAsync();
            var entities = await query.OrderByDescending(f => f.DateFiled)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResult<FiledCaseDTO>
            {
                Items = entities.Select(MapToDTO).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<FiledCaseDTO> GetByIdAsync(string id)
        {
            var entity = await GetTrackedFiledCase(id);
            return MapToDTO(entity);
        }

        public async Task<FiledCaseDTO> CreateAsync(AddFiledCaseDTO dto)
        {
            if (!Guid.TryParse(dto.ViolationId, out Guid violationId))
                throw new ArgumentException("Invalid Violation Id format.");

            var violation = await dbContext.Violations.FirstOrDefaultAsync(v => v.Id == violationId);
            if (violation == null)
                throw new KeyNotFoundException("Violation does not exist.");

            bool alreadyFiled = await dbContext.FiledCases.AnyAsync(f => f.ViolationId == violationId);
            if (alreadyFiled)
                throw new InvalidOperationException("A case is already filed for this violation.");

            var entity = new FiledCase
            {
                ViolationId = violationId,
                DateFiled = dto.DateFiled,
                NpsDocketNumber = dto.NpsDocketNumber?.Trim(),
                Remarks = dto.Remarks?.Trim()
            };
            await dbContext.FiledCases.AddAsync(entity);
            violation.Status = ViolationStatus.CaseFiled;
            await dbContext.SaveChangesAsync();
            return await GetByIdAsync(entity.Id.ToString());
        }

        public async Task<FiledCaseDTO> UpdateAsync(UpdateFiledCaseDTO dto)
        {
            var entity = await GetTrackedFiledCase(dto.FiledCaseId);

            entity.DateFiled = dto.DateFiled;
            entity.NpsDocketNumber = dto.NpsDocketNumber?.Trim();
            entity.Status = dto.Status;
            entity.DateResolved = dto.DateResolved;
            entity.ResolutionRemarks = dto.ResolutionRemarks?.Trim();
            entity.Remarks = dto.Remarks?.Trim();

            entity.Violation.Status = dto.Status switch
            {
                CaseStatus.Dismissed => ViolationStatus.Dismissed,
                CaseStatus.Closed => ViolationStatus.Closed,
                _ => ViolationStatus.CaseFiled
            };

            await dbContext.SaveChangesAsync();
            return await GetByIdAsync(entity.Id.ToString());
        }

        public async Task DeleteAsync(string id)
        {
            var entity = await GetTrackedFiledCase(id);
            entity.Violation.Status = ViolationStatus.Pending;
            dbContext.FiledCases.Remove(entity);
            await dbContext.SaveChangesAsync();
        }

        private async Task<FiledCase> GetTrackedFiledCase(string id)
        {
            if (!Guid.TryParse(id, out Guid parsedId))
                throw new ArgumentException("Invalid Filed Case Id format.");

            var entity = await IncludedQuery().FirstOrDefaultAsync(f => f.Id == parsedId);
            if (entity == null)
                throw new KeyNotFoundException("Filed case does not exist.");

            return entity;
        }
    }
}
