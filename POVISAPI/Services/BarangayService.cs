using Microsoft.EntityFrameworkCore;
using POVISAPI.Data;
using POVISAPI.Interfaces;
using POVISAPI.Models.DTO;
using POVISAPI.Models.Entity;

namespace POVISAPI.Services
{
    public class BarangayService : IBarangayService
    {
        private readonly AppDbContext dbContext;
        public BarangayService(AppDbContext _dbContext) => dbContext = _dbContext;

        private static BarangayDTO MapToDTO(Barangay b) => new()
        {
            BarangayId = b.Id.ToString(),
            Name = b.Name,
            District = b.District,
            IsActive = b.IsActive
        };

        public async Task<List<BarangayDTO>> GetAllAsync(bool includeInactive = false)
        {
            var query = dbContext.Barangays.AsNoTracking().AsQueryable();
            if (!includeInactive) query = query.Where(b => b.IsActive);

            var entities = await query.OrderBy(b => b.District).ThenBy(b => b.Name).ToListAsync();
            return entities.Select(MapToDTO).ToList();
        }

        public async Task<BarangayDTO> GetByIdAsync(string id)
        {
            var barangay = await GetTrackedBarangay(id);
            return MapToDTO(barangay);
        }

        public async Task<BarangayDTO> CreateAsync(AddBarangayDTO dto)
        {
            var entity = new Barangay { Name = dto.Name.Trim(), District = dto.District };
            await dbContext.Barangays.AddAsync(entity);
            await dbContext.SaveChangesAsync();
            return MapToDTO(entity);
        }

        public async Task<BarangayDTO> UpdateAsync(UpdateBarangayDTO dto)
        {
            var entity = await GetTrackedBarangay(dto.BarangayId);
            entity.Name = dto.Name.Trim();
            entity.District = dto.District;
            entity.IsActive = dto.IsActive;
            await dbContext.SaveChangesAsync();
            return MapToDTO(entity);
        }

        public async Task DeleteAsync(string id)
        {
            var entity = await GetTrackedBarangay(id);

            var isReferenced = await dbContext.Violations.AnyAsync(v => v.BarangayOfOccurrenceId == entity.Id)
                            || await dbContext.Violators.AnyAsync(v => v.BarangayId == entity.Id);
            if (isReferenced)
            {
                entity.IsActive = false;
            }
            else
            {
                dbContext.Barangays.Remove(entity);
            }
            await dbContext.SaveChangesAsync();
        }

        private async Task<Barangay> GetTrackedBarangay(string id)
        {
            if (!Guid.TryParse(id, out Guid parsedId))
                throw new ArgumentException("Invalid Barangay Id format.");

            var entity = await dbContext.Barangays.FirstOrDefaultAsync(b => b.Id == parsedId);
            if (entity == null)
                throw new KeyNotFoundException("Barangay does not exist.");

            return entity;
        }
    }
}
