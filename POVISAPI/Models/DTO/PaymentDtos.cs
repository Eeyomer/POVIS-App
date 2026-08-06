using System.ComponentModel.DataAnnotations;

namespace POVISAPI.Models.DTO
{
    public class PaymentDTO
    {
        public string PaymentId { get; set; } = string.Empty;
        public string ViolationId { get; set; } = string.Empty;
        public string OvrNumber { get; set; } = string.Empty;
        public string ViolatorFullName { get; set; } = string.Empty;
        public DateOnly DateOfPayment { get; set; }
        public decimal Amount { get; set; }
        public string? ReceiptNumber { get; set; }
        public string? PaidAt { get; set; }
        public string? Remarks { get; set; }
    }

    public class AddPaymentDTO
    {
        [Required(ErrorMessage = "Violation is required")]
        public required string ViolationId { get; set; }

        [Required(ErrorMessage = "Date of payment is required")]
        public DateOnly DateOfPayment { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, 1000000, ErrorMessage = "Amount must be between 0.01 and 1,000,000")]
        public decimal Amount { get; set; }

        [StringLength(50, ErrorMessage = "Receipt number cannot exceed 50 characters")]
        public string? ReceiptNumber { get; set; }

        [StringLength(100, ErrorMessage = "Paid at cannot exceed 100 characters")]
        public string? PaidAt { get; set; }

        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters")]
        public string? Remarks { get; set; }
    }

    public class UpdatePaymentDTO
    {
        [Required(ErrorMessage = "PaymentId is required")]
        public required string PaymentId { get; set; }

        [Required(ErrorMessage = "Date of payment is required")]
        public DateOnly DateOfPayment { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, 1000000, ErrorMessage = "Amount must be between 0.01 and 1,000,000")]
        public decimal Amount { get; set; }

        [StringLength(50, ErrorMessage = "Receipt number cannot exceed 50 characters")]
        public string? ReceiptNumber { get; set; }

        [StringLength(100, ErrorMessage = "Paid at cannot exceed 100 characters")]
        public string? PaidAt { get; set; }

        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters")]
        public string? Remarks { get; set; }
    }
}
