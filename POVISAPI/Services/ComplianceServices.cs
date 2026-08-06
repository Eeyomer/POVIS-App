using Microsoft.EntityFrameworkCore;
using POVISAPI.Data;
using POVISAPI.Interfaces;
using POVISAPI.Models.DTO;
using POVISAPI.Models.Entity;
using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Services
{
    public class SeminarRecordService : ISeminarRecordService
    {
        private readonly AppDbContext dbContext;
        public SeminarRecordService(AppDbContext _dbContext) => dbContext = _dbContext;

        private static SeminarRecordDTO MapToDTO(SeminarRecord s) => new()
        {
            SeminarRecordId = s.Id.ToString(),
            ViolationId = s.ViolationId.ToString(),
            OvrNumber = s.Violation.OvrNumber,
            ViolatorFullName = s.Violation.Violator.LastName + ", " + s.Violation.Violator.FirstName,
            SeminarType = s.SeminarType,
            ScheduledDate = s.ScheduledDate,
            RequiredDays = s.RequiredDays,
            Status = s.Status,
            DateCompleted = s.DateCompleted,
            Remarks = s.Remarks
        };

        private IQueryable<SeminarRecord> IncludedQuery() =>
            dbContext.SeminarRecords.Include(s => s.Violation).ThenInclude(v => v.Violator);

        public async Task<PagedResult<SeminarRecordDTO>> GetAllAsync(SeminarType? type, ComplianceStatus? status, int page = 1, int pageSize = 20)
        {
            var query = IncludedQuery().AsNoTracking().AsQueryable();
            if (type.HasValue) query = query.Where(s => s.SeminarType == type);
            if (status.HasValue) query = query.Where(s => s.Status == status);

            var total = await query.CountAsync();
            var entities = await query.OrderBy(s => s.ScheduledDate)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResult<SeminarRecordDTO>
            {
                Items = entities.Select(MapToDTO).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<SeminarRecordDTO> GetByIdAsync(string id)
        {
            var entity = await GetTrackedRecord(id);
            return MapToDTO(entity);
        }

        public async Task<SeminarRecordDTO> CreateAsync(AddSeminarRecordDTO dto)
        {
            if (!Guid.TryParse(dto.ViolationId, out Guid violationId))
                throw new ArgumentException("Invalid Violation Id format.");

            var violation = await dbContext.Violations.FirstOrDefaultAsync(v => v.Id == violationId);
            if (violation == null)
                throw new KeyNotFoundException("Violation does not exist.");

            var entity = new SeminarRecord
            {
                ViolationId = violationId,
                SeminarType = dto.SeminarType,
                ScheduledDate = dto.ScheduledDate,
                RequiredDays = dto.RequiredDays,
                Remarks = dto.Remarks?.Trim()
            };
            await dbContext.SeminarRecords.AddAsync(entity);
            violation.Status = ViolationStatus.ForSeminar;
            await dbContext.SaveChangesAsync();
            return await GetByIdAsync(entity.Id.ToString());
        }

        public async Task<SeminarRecordDTO> UpdateAsync(UpdateSeminarRecordDTO dto)
        {
            var entity = await GetTrackedRecord(dto.SeminarRecordId);
            entity.SeminarType = dto.SeminarType;
            entity.ScheduledDate = dto.ScheduledDate;
            entity.RequiredDays = dto.RequiredDays;
            entity.Status = dto.Status;
            entity.DateCompleted = dto.DateCompleted;
            entity.Remarks = dto.Remarks?.Trim();

            if (dto.Status == ComplianceStatus.Completed)
                entity.Violation.Status = ViolationStatus.Closed;
            else if (dto.Status == ComplianceStatus.DidNotComply)
                entity.Violation.Status = ViolationStatus.DidNotComply;

            await dbContext.SaveChangesAsync();
            return await GetByIdAsync(entity.Id.ToString());
        }

        public async Task DeleteAsync(string id)
        {
            var entity = await GetTrackedRecord(id);
            dbContext.SeminarRecords.Remove(entity);
            await dbContext.SaveChangesAsync();
        }

        private async Task<SeminarRecord> GetTrackedRecord(string id)
        {
            if (!Guid.TryParse(id, out Guid parsedId))
                throw new ArgumentException("Invalid Seminar Record Id format.");

            var entity = await IncludedQuery().FirstOrDefaultAsync(s => s.Id == parsedId);
            if (entity == null)
                throw new KeyNotFoundException("Seminar record does not exist.");

            return entity;
        }
    }

    public class CommunityServiceRecordService : ICommunityServiceRecordService
    {
        private readonly AppDbContext dbContext;
        public CommunityServiceRecordService(AppDbContext _dbContext) => dbContext = _dbContext;

        private static CommunityServiceDTO MapToDTO(CommunityServiceRecord c) => new()
        {
            CommunityServiceRecordId = c.Id.ToString(),
            ViolationId = c.ViolationId.ToString(),
            OvrNumber = c.Violation.OvrNumber,
            ViolatorFullName = c.Violation.Violator.LastName + ", " + c.Violation.Violator.FirstName,
            RequiredHours = c.RequiredHours,
            RequiredDays = c.RequiredDays,
            ScheduledDate = c.ScheduledDate,
            DateCompleted = c.DateCompleted,
            Status = c.Status,
            AssignedLocation = c.AssignedLocation,
            Remarks = c.Remarks
        };

        private IQueryable<CommunityServiceRecord> IncludedQuery() =>
            dbContext.CommunityServiceRecords.Include(c => c.Violation).ThenInclude(v => v.Violator);

        public async Task<PagedResult<CommunityServiceDTO>> GetAllAsync(ComplianceStatus? status, int page = 1, int pageSize = 20)
        {
            var query = IncludedQuery().AsNoTracking().AsQueryable();
            if (status.HasValue) query = query.Where(c => c.Status == status);

            var total = await query.CountAsync();
            var entities = await query.OrderBy(c => c.ScheduledDate)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResult<CommunityServiceDTO>
            {
                Items = entities.Select(MapToDTO).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<CommunityServiceDTO> GetByIdAsync(string id)
        {
            var entity = await GetTrackedRecord(id);
            return MapToDTO(entity);
        }

        public async Task<CommunityServiceDTO> CreateAsync(AddCommunityServiceDTO dto)
        {
            if (!Guid.TryParse(dto.ViolationId, out Guid violationId))
                throw new ArgumentException("Invalid Violation Id format.");

            var violation = await dbContext.Violations.FirstOrDefaultAsync(v => v.Id == violationId);
            if (violation == null)
                throw new KeyNotFoundException("Violation does not exist.");

            var entity = new CommunityServiceRecord
            {
                ViolationId = violationId,
                RequiredHours = dto.RequiredHours,
                RequiredDays = dto.RequiredDays,
                ScheduledDate = dto.ScheduledDate,
                AssignedLocation = dto.AssignedLocation?.Trim(),
                Remarks = dto.Remarks?.Trim()
            };
            await dbContext.CommunityServiceRecords.AddAsync(entity);
            violation.Status = ViolationStatus.ForCommunityService;
            await dbContext.SaveChangesAsync();
            return await GetByIdAsync(entity.Id.ToString());
        }

        public async Task<CommunityServiceDTO> UpdateAsync(UpdateCommunityServiceDTO dto)
        {
            var entity = await GetTrackedRecord(dto.CommunityServiceRecordId);
            entity.RequiredHours = dto.RequiredHours;
            entity.RequiredDays = dto.RequiredDays;
            entity.ScheduledDate = dto.ScheduledDate;
            entity.DateCompleted = dto.DateCompleted;
            entity.Status = dto.Status;
            entity.AssignedLocation = dto.AssignedLocation?.Trim();
            entity.Remarks = dto.Remarks?.Trim();

            if (dto.Status == ComplianceStatus.Completed)
                entity.Violation.Status = ViolationStatus.Closed;
            else if (dto.Status == ComplianceStatus.DidNotComply)
                entity.Violation.Status = ViolationStatus.DidNotComply;

            await dbContext.SaveChangesAsync();
            return await GetByIdAsync(entity.Id.ToString());
        }

        public async Task DeleteAsync(string id)
        {
            var entity = await GetTrackedRecord(id);
            dbContext.CommunityServiceRecords.Remove(entity);
            await dbContext.SaveChangesAsync();
        }

        private async Task<CommunityServiceRecord> GetTrackedRecord(string id)
        {
            if (!Guid.TryParse(id, out Guid parsedId))
                throw new ArgumentException("Invalid Community Service Record Id format.");

            var entity = await IncludedQuery().FirstOrDefaultAsync(c => c.Id == parsedId);
            if (entity == null)
                throw new KeyNotFoundException("Community service record does not exist.");

            return entity;
        }
    }

    public class CounselingRecordService : ICounselingRecordService
    {
        private readonly AppDbContext dbContext;
        public CounselingRecordService(AppDbContext _dbContext) => dbContext = _dbContext;

        private static CounselingDTO MapToDTO(CounselingRecord c) => new()
        {
            CounselingRecordId = c.Id.ToString(),
            ViolationId = c.ViolationId.ToString(),
            OvrNumber = c.Violation.OvrNumber,
            ViolatorFullName = c.Violation.Violator.LastName + ", " + c.Violation.Violator.FirstName,
            InterventionType = c.InterventionType,
            ConductedBy = c.ConductedBy,
            ScheduledDate = c.ScheduledDate,
            DateCompleted = c.DateCompleted,
            Status = c.Status,
            ParentOrGuardianPresent = c.ParentOrGuardianPresent,
            Remarks = c.Remarks
        };

        private IQueryable<CounselingRecord> IncludedQuery() =>
            dbContext.CounselingRecords.Include(c => c.Violation).ThenInclude(v => v.Violator);

        public async Task<PagedResult<CounselingDTO>> GetAllAsync(ComplianceStatus? status, int page = 1, int pageSize = 20)
        {
            var query = IncludedQuery().AsNoTracking().AsQueryable();
            if (status.HasValue) query = query.Where(c => c.Status == status);

            var total = await query.CountAsync();
            var entities = await query.OrderBy(c => c.ScheduledDate)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResult<CounselingDTO>
            {
                Items = entities.Select(MapToDTO).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<CounselingDTO> GetByIdAsync(string id)
        {
            var entity = await GetTrackedRecord(id);
            return MapToDTO(entity);
        }

        public async Task<CounselingDTO> CreateAsync(AddCounselingDTO dto)
        {
            if (!Guid.TryParse(dto.ViolationId, out Guid violationId))
                throw new ArgumentException("Invalid Violation Id format.");

            var violation = await dbContext.Violations.FirstOrDefaultAsync(v => v.Id == violationId);
            if (violation == null)
                throw new KeyNotFoundException("Violation does not exist.");

            var entity = new CounselingRecord
            {
                ViolationId = violationId,
                InterventionType = dto.InterventionType?.Trim(),
                ConductedBy = dto.ConductedBy?.Trim(),
                ScheduledDate = dto.ScheduledDate,
                ParentOrGuardianPresent = dto.ParentOrGuardianPresent,
                Remarks = dto.Remarks?.Trim()
            };
            await dbContext.CounselingRecords.AddAsync(entity);
            violation.Status = ViolationStatus.ForCounseling;
            await dbContext.SaveChangesAsync();
            return await GetByIdAsync(entity.Id.ToString());
        }

        public async Task<CounselingDTO> UpdateAsync(UpdateCounselingDTO dto)
        {
            var entity = await GetTrackedRecord(dto.CounselingRecordId);
            entity.InterventionType = dto.InterventionType?.Trim();
            entity.ConductedBy = dto.ConductedBy?.Trim();
            entity.ScheduledDate = dto.ScheduledDate;
            entity.DateCompleted = dto.DateCompleted;
            entity.Status = dto.Status;
            entity.ParentOrGuardianPresent = dto.ParentOrGuardianPresent;
            entity.Remarks = dto.Remarks?.Trim();

            if (dto.Status == ComplianceStatus.Completed)
                entity.Violation.Status = ViolationStatus.Closed;
            else if (dto.Status == ComplianceStatus.DidNotComply)
                entity.Violation.Status = ViolationStatus.DidNotComply;

            await dbContext.SaveChangesAsync();
            return await GetByIdAsync(entity.Id.ToString());
        }

        public async Task DeleteAsync(string id)
        {
            var entity = await GetTrackedRecord(id);
            dbContext.CounselingRecords.Remove(entity);
            await dbContext.SaveChangesAsync();
        }

        private async Task<CounselingRecord> GetTrackedRecord(string id)
        {
            if (!Guid.TryParse(id, out Guid parsedId))
                throw new ArgumentException("Invalid Counseling Record Id format.");

            var entity = await IncludedQuery().FirstOrDefaultAsync(c => c.Id == parsedId);
            if (entity == null)
                throw new KeyNotFoundException("Counseling record does not exist.");

            return entity;
        }
    }
}
