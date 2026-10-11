using POVISAPI.Models.DTO;

namespace POVISAPI.Interfaces
{
    public interface IPaymentService
    {
        Task<PagedResult<PaymentDTO>> GetAllAsync(DateOnly? from, DateOnly? to, int page = 1, int pageSize = 20, string? search = null);
        Task<PaymentDTO> GetByIdAsync(string id);
        Task<PaymentDTO> GetByViolationIdAsync(string violationId);
        Task<PaymentDTO> CreateAsync(AddPaymentDTO dto);
        Task<PaymentDTO> UpdateAsync(UpdatePaymentDTO dto);
        Task DeleteAsync(string id);
    }
}
