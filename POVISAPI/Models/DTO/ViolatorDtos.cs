using POVISAPI.Models.Entity.Enums;
using System.ComponentModel.DataAnnotations;

namespace POVISAPI.Models.DTO
{
    public class ViolatorDTO
    {
        public string ViolatorId { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string? Suffix { get; set; }
        public Gender Gender { get; set; }
        public DateOnly? Birthday { get; set; }
        public string? Address { get; set; }
        public string? BarangayId { get; set; }
        public string? BarangayName { get; set; }
        public string? City { get; set; }
        public string? PresentedValidId { get; set; }
        public string? ContactNumber { get; set; }
        public int ViolationCount { get; set; }
    }

    public class AddViolatorDTO
    {
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        public required string LastName { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public required string FirstName { get; set; }

        [StringLength(100, ErrorMessage = "Middle name cannot exceed 100 characters")]
        public string? MiddleName { get; set; }

        [StringLength(20, ErrorMessage = "Suffix cannot exceed 20 characters")]
        public string? Suffix { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [EnumDataType(typeof(Gender), ErrorMessage = "Invalid gender")]
        public Gender Gender { get; set; }

        public DateOnly? Birthday { get; set; }

        [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters")]
        public string? Address { get; set; }

        public string? BarangayId { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string? City { get; set; }

        [StringLength(100, ErrorMessage = "Presented valid ID cannot exceed 100 characters")]
        public string? PresentedValidId { get; set; }

        [StringLength(30, ErrorMessage = "Contact number cannot exceed 30 characters")]
        [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "Contact number contains invalid characters")]
        public string? ContactNumber { get; set; }
    }

    public class UpdateViolatorDTO
    {
        [Required(ErrorMessage = "ViolatorId is required")]
        public required string ViolatorId { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        public required string LastName { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public required string FirstName { get; set; }

        [StringLength(100, ErrorMessage = "Middle name cannot exceed 100 characters")]
        public string? MiddleName { get; set; }

        [StringLength(20, ErrorMessage = "Suffix cannot exceed 20 characters")]
        public string? Suffix { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [EnumDataType(typeof(Gender), ErrorMessage = "Invalid gender")]
        public Gender Gender { get; set; }

        public DateOnly? Birthday { get; set; }

        [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters")]
        public string? Address { get; set; }

        public string? BarangayId { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string? City { get; set; }

        [StringLength(100, ErrorMessage = "Presented valid ID cannot exceed 100 characters")]
        public string? PresentedValidId { get; set; }

        [StringLength(30, ErrorMessage = "Contact number cannot exceed 30 characters")]
        [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "Contact number contains invalid characters")]
        public string? ContactNumber { get; set; }
    }

    public class ViolatorHistoryDTO
    {
        public string ViolationId { get; set; } = string.Empty;
        public string OvrNumber { get; set; } = string.Empty;
        public DateOnly DateOfOccurrence { get; set; }
        public string OrdinanceShortName { get; set; } = string.Empty;
        public OffenseLevel OffenseLevel { get; set; }
        public ViolationStatus Status { get; set; }
    }
}
