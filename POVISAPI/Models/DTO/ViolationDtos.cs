using POVISAPI.Models.Entity.Enums;
using System.ComponentModel.DataAnnotations;

namespace POVISAPI.Models.DTO
{
    public class ViolationEvidenceDTO
    {
        public string EvidenceId { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string ImageData { get; set; } = string.Empty;
    }

    public class ViolationDTO
    {
        public string ViolationId { get; set; } = string.Empty;
        public string ViolatorId { get; set; } = string.Empty;
        public string ViolatorFullName { get; set; } = string.Empty;
        public string OvrNumber { get; set; } = string.Empty;
        public DateOnly DateOfSubmission { get; set; }
        public Division Division { get; set; }
        public int? AgeAtApprehension { get; set; }
        public DateOnly DateOfOccurrence { get; set; }
        public TimeOnly? TimeOfOccurrence { get; set; }
        public string? PlaceOfOccurrence { get; set; }
        public string? BarangayOfOccurrenceId { get; set; }
        public string? BarangayOfOccurrenceName { get; set; }
        public string? Witness { get; set; }
        public string? ApprehendingOfficer { get; set; }
        public string? ApprehendingOfficerDeputationNumber { get; set; }
        public int EvidenceCount { get; set; }

        /// <summary>Only populated when a single violation is fetched, not in lists.</summary>
        public List<ViolationEvidenceDTO> Evidence { get; set; } = new();
        public string OrdinanceId { get; set; } = string.Empty;
        public string OrdinanceShortName { get; set; } = string.Empty;
        public string? OrdinanceSectionId { get; set; }
        public string? OrdinanceSectionCode { get; set; }
        public OffenseLevel OffenseLevel { get; set; }
        public bool IsWarningTicket { get; set; }
        public ViolationStatus Status { get; set; }
        public string? Remarks { get; set; }
        public string? Remarks2 { get; set; }
        public bool HasPayment { get; set; }
        public bool HasFiledCase { get; set; }
        public bool IsReviewed { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewedBy { get; set; }
    }

    public class AddViolationDTO
    {
        [Required(ErrorMessage = "Violator is required")]
        public required string ViolatorId { get; set; }

        [Required(ErrorMessage = "OVR number is required")]
        [StringLength(20, ErrorMessage = "OVR number cannot exceed 20 characters")]
        public required string OvrNumber { get; set; }

        [Required(ErrorMessage = "Date of submission is required")]
        public DateOnly DateOfSubmission { get; set; }

        [Required(ErrorMessage = "Division is required")]
        [EnumDataType(typeof(Division), ErrorMessage = "Invalid division")]
        public Division Division { get; set; }

        [Range(1, 120, ErrorMessage = "Age must be between 1 and 120")]
        public int? AgeAtApprehension { get; set; }

        [Required(ErrorMessage = "Date of occurrence is required")]
        public DateOnly DateOfOccurrence { get; set; }

        public TimeOnly? TimeOfOccurrence { get; set; }

        [StringLength(300, ErrorMessage = "Place of occurrence cannot exceed 300 characters")]
        public string? PlaceOfOccurrence { get; set; }

        public string? BarangayOfOccurrenceId { get; set; }

        [StringLength(150, ErrorMessage = "Witness cannot exceed 150 characters")]
        public string? Witness { get; set; }

        [StringLength(150, ErrorMessage = "Apprehending officer cannot exceed 150 characters")]
        public string? ApprehendingOfficer { get; set; }

        [StringLength(50, ErrorMessage = "Deputation number cannot exceed 50 characters")]
        public string? ApprehendingOfficerDeputationNumber { get; set; }

        /// <summary>Evidence photos as base64 data URLs (JPEG/JPG/PNG), at most 5.</summary>
        public List<string>? EvidencePhotos { get; set; }

        [Required(ErrorMessage = "Ordinance is required")]
        public required string OrdinanceId { get; set; }

        public string? OrdinanceSectionId { get; set; }

        public bool IsWarningTicket { get; set; } = false;

        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters")]
        public string? Remarks { get; set; }

        [StringLength(500, ErrorMessage = "Remarks II cannot exceed 500 characters")]
        public string? Remarks2 { get; set; }
    }

    public class UpdateViolationDTO
    {
        [Required(ErrorMessage = "ViolationId is required")]
        public required string ViolationId { get; set; }

        [Required(ErrorMessage = "Date of submission is required")]
        public DateOnly DateOfSubmission { get; set; }

        [Required(ErrorMessage = "Division is required")]
        [EnumDataType(typeof(Division), ErrorMessage = "Invalid division")]
        public Division Division { get; set; }

        [Range(1, 120, ErrorMessage = "Age must be between 1 and 120")]
        public int? AgeAtApprehension { get; set; }

        [Required(ErrorMessage = "Date of occurrence is required")]
        public DateOnly DateOfOccurrence { get; set; }

        public TimeOnly? TimeOfOccurrence { get; set; }

        [StringLength(300, ErrorMessage = "Place of occurrence cannot exceed 300 characters")]
        public string? PlaceOfOccurrence { get; set; }

        public string? BarangayOfOccurrenceId { get; set; }

        [StringLength(150, ErrorMessage = "Witness cannot exceed 150 characters")]
        public string? Witness { get; set; }

        [StringLength(150, ErrorMessage = "Apprehending officer cannot exceed 150 characters")]
        public string? ApprehendingOfficer { get; set; }

        [StringLength(50, ErrorMessage = "Deputation number cannot exceed 50 characters")]
        public string? ApprehendingOfficerDeputationNumber { get; set; }

        /// <summary>New evidence photos as base64 data URLs (JPEG/JPG/PNG). Total per violation is capped at 5.</summary>
        public List<string>? NewEvidencePhotos { get; set; }

        /// <summary>Ids of existing evidence photos to delete.</summary>
        public List<string>? RemoveEvidenceIds { get; set; }

        public string? OrdinanceSectionId { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [EnumDataType(typeof(ViolationStatus), ErrorMessage = "Invalid status")]
        public ViolationStatus Status { get; set; }

        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters")]
        public string? Remarks { get; set; }

        [StringLength(500, ErrorMessage = "Remarks II cannot exceed 500 characters")]
        public string? Remarks2 { get; set; }
    }

    public class ViolationFilterDTO
    {
        public string? OrdinanceId { get; set; }
        public string? BarangayId { get; set; }
        public ViolationStatus? Status { get; set; }
        public DateOnly? DateFrom { get; set; }
        public DateOnly? DateTo { get; set; }

        [StringLength(100, ErrorMessage = "Search term cannot exceed 100 characters")]
        public string? Search { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1")]
        public int Page { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
        public int PageSize { get; set; } = 20;
    }

    public class SetReviewedDTO
    {
        public bool Reviewed { get; set; }
    }

    public class ApplicablePenaltyDTO
    {
        public OffenseLevel OffenseLevel { get; set; }
        public decimal? FineAmount { get; set; }
        public string PenaltyText { get; set; } = string.Empty;
    }
}
