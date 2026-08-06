using POVISAPI.Models.Entity.Enums;
using System.ComponentModel.DataAnnotations;

namespace POVISAPI.Models.DTO
{
    public class FiledCaseDTO
    {
        public string FiledCaseId { get; set; } = string.Empty;
        public string ViolationId { get; set; } = string.Empty;
        public string OvrNumber { get; set; } = string.Empty;
        public string ViolatorFullName { get; set; } = string.Empty;
        public DateOnly DateFiled { get; set; }
        public string? NpsDocketNumber { get; set; }
        public CaseStatus Status { get; set; }
        public DateOnly? DateResolved { get; set; }
        public string? ResolutionRemarks { get; set; }
        public string? Remarks { get; set; }
    }

    public class AddFiledCaseDTO
    {
        [Required(ErrorMessage = "Violation is required")]
        public required string ViolationId { get; set; }

        [Required(ErrorMessage = "Date filed is required")]
        public DateOnly DateFiled { get; set; }

        [StringLength(50, ErrorMessage = "NPS docket number cannot exceed 50 characters")]
        public string? NpsDocketNumber { get; set; }

        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters")]
        public string? Remarks { get; set; }
    }

    public class UpdateFiledCaseDTO
    {
        [Required(ErrorMessage = "FiledCaseId is required")]
        public required string FiledCaseId { get; set; }

        [Required(ErrorMessage = "Date filed is required")]
        public DateOnly DateFiled { get; set; }

        [StringLength(50, ErrorMessage = "NPS docket number cannot exceed 50 characters")]
        public string? NpsDocketNumber { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [EnumDataType(typeof(CaseStatus), ErrorMessage = "Invalid case status")]
        public CaseStatus Status { get; set; }

        public DateOnly? DateResolved { get; set; }

        [StringLength(500, ErrorMessage = "Resolution remarks cannot exceed 500 characters")]
        public string? ResolutionRemarks { get; set; }

        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters")]
        public string? Remarks { get; set; }
    }
}
