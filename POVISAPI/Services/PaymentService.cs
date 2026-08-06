using Microsoft.EntityFrameworkCore;
using POVISAPI.Data;
using POVISAPI.Interfaces;
using POVISAPI.Models.DTO;
using POVISAPI.Models.Entity;
using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext dbContext;
        public PaymentService(AppDbContext _dbContext) => dbContext = _dbContext;

        private static PaymentDTO MapToDTO(Payment p) => new()
        {
            PaymentId = p.Id.ToString(),
            ViolationId = p.ViolationId.ToString(),
            OvrNumber = p.Violation.OvrNumber,
            ViolatorFullName = p.Violation.Violator.LastName + ", " + p.Violation.Violator.FirstName,
            DateOfPayment = p.DateOfPayment,
            Amount = p.Amount,
            ReceiptNumber = p.ReceiptNumber,
            PaidAt = p.PaidAt,
            Remarks = p.Remarks
        };

        private IQueryable<Payment> IncludedQuery() =>
            dbContext.Payments.Include(p => p.Violation).ThenInclude(v => v.Violator);

        public async Task<PagedResult<PaymentDTO>> GetAllAsync(DateOnly? from, DateOnly? to, int page = 1, int pageSize = 20)
        {
            var query = IncludedQuery().AsNoTracking().AsQueryable();
            if (from.HasValue) query = query.Where(p => p.DateOfPayment >= from);
            if (to.HasValue) query = query.Where(p => p.DateOfPayment <= to);

            var total = await query.CountAsync();
            var entities = await query.OrderByDescending(p => p.DateOfPayment)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResult<PaymentDTO>
            {
                Items = entities.Select(MapToDTO).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PaymentDTO> GetByIdAsync(string id)
        {
            var entity = await GetTrackedPayment(id);
            return MapToDTO(entity);
        }

        public async Task<PaymentDTO> GetByViolationIdAsync(string violationId)
        {
            if (!Guid.TryParse(violationId, out Guid parsedId))
                throw new ArgumentException("Invalid Violation Id format.");

            var entity = await IncludedQuery().FirstOrDefaultAsync(p => p.ViolationId == parsedId);
            if (entity == null)
                throw new KeyNotFoundException("No payment found for this violation.");

            return MapToDTO(entity);
        }

        public async Task<PaymentDTO> CreateAsync(AddPaymentDTO dto)
        {
            if (!Guid.TryParse(dto.ViolationId, out Guid violationId))
                throw new ArgumentException("Invalid Violation Id format.");

            var violation = await dbContext.Violations
                .Include(v => v.FiledCase)
                .FirstOrDefaultAsync(v => v.Id == violationId);
            if (violation == null)
                throw new KeyNotFoundException("Violation does not exist.");

            bool alreadyPaid = await dbContext.Payments.AnyAsync(p => p.ViolationId == violationId);
            if (alreadyPaid)
                throw new InvalidOperationException("This violation already has a payment.");

            var entity = new Payment
            {
                ViolationId = violationId,
                DateOfPayment = dto.DateOfPayment,
                Amount = dto.Amount,
                ReceiptNumber = dto.ReceiptNumber?.Trim(),
                PaidAt = dto.PaidAt?.Trim(),
                Remarks = dto.Remarks?.Trim()
            };
            await dbContext.Payments.AddAsync(entity);

            violation.Status = violation.FiledCase is not null
                ? ViolationStatus.Closed
                : ViolationStatus.Paid;

            await dbContext.SaveChangesAsync();
            return await GetByIdAsync(entity.Id.ToString());
        }

        public async Task<PaymentDTO> UpdateAsync(UpdatePaymentDTO dto)
        {
            var entity = await GetTrackedPayment(dto.PaymentId);
            entity.DateOfPayment = dto.DateOfPayment;
            entity.Amount = dto.Amount;
            entity.ReceiptNumber = dto.ReceiptNumber?.Trim();
            entity.PaidAt = dto.PaidAt?.Trim();
            entity.Remarks = dto.Remarks?.Trim();
            await dbContext.SaveChangesAsync();
            return await GetByIdAsync(entity.Id.ToString());
        }

        public async Task DeleteAsync(string id)
        {
            var entity = await GetTrackedPayment(id);
            entity.Violation.Status = ViolationStatus.Pending;
            dbContext.Payments.Remove(entity);
            await dbContext.SaveChangesAsync();
        }

        private async Task<Payment> GetTrackedPayment(string id)
        {
            if (!Guid.TryParse(id, out Guid parsedId))
                throw new ArgumentException("Invalid Payment Id format.");

            var entity = await IncludedQuery().FirstOrDefaultAsync(p => p.Id == parsedId);
            if (entity == null)
                throw new KeyNotFoundException("Payment does not exist.");

            return entity;
        }
    }
}
