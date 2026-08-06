using POVISAPI.Models.Entity.Enums;
using System.ComponentModel.DataAnnotations;

namespace POVISAPI.Models.DTO
{
    // ----- Seminar (SMOKING SEMINAR / SEMINAR VAPING sheets) -----
    public class SeminarRecordDTO
    {
        public string SeminarRecordId { get; set; } = string.Empty;
        public string ViolationId { get; set; } = string.Empty;
        public string OvrNumber { get; set; } = string.Empty;
        public string ViolatorFullName { get; set; } = string.Empty;
        public SeminarType SeminarType { get; set; }
        public DateOnly? ScheduledDate { get; set; }
        public decimal? RequiredDays { get; set; }
        public ComplianceStatus Status { get; set; }
        public DateOnly? DateCompleted { get; set; }
        public string? Remarks { get; set; }
    }

    public class AddSeminarRecordDTO
    {
        [Required(ErrorMessage = "Violation is required")]
        public required string ViolationId { get; set; }

        [Required(ErrorMessage = "Seminar type is required")]
        [EnumDataType(typeof(SeminarType), ErrorMessage = "Invalid seminar type")]
        public SeminarType SeminarType { get; set; }

        public DateOnly? ScheduledDate { get; set; }

        [Range(0.5, 365, ErrorMessage = "Required days must be between 0.5 and 365")]
        public decimal? RequiredDays { get; set; }

        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters")]
        public string? Remarks { get; set; }
    }

    public class UpdateSeminarRecordDTO
    {
        [Required(ErrorMessage = "SeminarRecordId is required")]
        public required string SeminarRecordId { get; set; }

        [Required(ErrorMessage = "Seminar type is required")]
        [EnumDataType(typeof(SeminarType), ErrorMessage = "Invalid seminar type")]
        public SeminarType SeminarType { get; set; }

        public DateOnly? ScheduledDate { get; set; }

        [Range(0.5, 365, ErrorMessage = "Required days must be between 0.5 and 365")]
        public decimal? RequiredDays { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [EnumDataType(typeof(ComplianceStatus), ErrorMessage = "Invalid status")]
        public ComplianceStatus Status { get; set; }

        public DateOnly? DateCompleted { get; set; }

        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters")]
        public string? Remarks { get; set; }
    }

    // ----- Community Service -----
    public class CommunityServiceDTO
    {
        public string CommunityServiceRecordId { get; set; } = string.Empty;
        public string ViolationId { get; set; } = string.Empty;
        public string OvrNumber { get; set; } = string.Empty;
        public string ViolatorFullName { get; set; } = string.Empty;
        public decimal? RequiredHours { get; set; }
        public int? RequiredDays { get; set; }
        public DateOnly? ScheduledDate { get; set; }
        public DateOnly? DateCompleted { get; set; }
        public ComplianceStatus Status { get; set; }
        public string? AssignedLocation { get; set; }
        public string? Remarks { get; set; }
    }

    public class AddCommunityServiceDTO
    {
        [Required(ErrorMessage = "Violation is required")]
        public required string ViolationId { get; set; }

        [Range(0.5, 1000, ErrorMessage = "Required hours must be between 0.5 and 1,000")]
        public decimal? RequiredHours { get; set; }

        [Range(1, 365, ErrorMessage = "Required days must be between 1 and 365")]
        public int? RequiredDays { get; set; }

        public DateOnly? ScheduledDate { get; set; }

        [StringLength(200, ErrorMessage = "Assigned location cannot exceed 200 characters")]
        public string? AssignedLocation { get; set; }

        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters")]
        public string? Remarks { get; set; }
    }

    public class UpdateCommunityServiceDTO
    {
        [Required(ErrorMessage = "CommunityServiceRecordId is required")]
        public required string CommunityServiceRecordId { get; set; }

        [Range(0.5, 1000, ErrorMessage = "Required hours must be between 0.5 and 1,000")]
        public decimal? RequiredHours { get; set; }

        [Range(1, 365, ErrorMessage = "Required days must be between 1 and 365")]
        public int? RequiredDays { get; set; }

        public DateOnly? ScheduledDate { get; set; }

        public DateOnly? DateCompleted { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [EnumDataType(typeof(ComplianceStatus), ErrorMessage = "Invalid status")]
        public ComplianceStatus Status { get; set; }

        [StringLength(200, ErrorMessage = "Assigned location cannot exceed 200 characters")]
        public string? AssignedLocation { get; set; }

        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters")]
        public string? Remarks { get; set; }
    }

    // ----- Counseling (REACH OUT MINORS) -----
    public class CounselingDTO
    {
        public string CounselingRecordId { get; set; } = string.Empty;
        public string ViolationId { get; set; } = string.Empty;
        public string OvrNumber { get; set; } = string.Empty;
        public string ViolatorFullName { get; set; } = string.Empty;
        public string? InterventionType { get; set; }
        public string? ConductedBy { get; set; }
        public DateOnly? ScheduledDate { get; set; }
        public DateOnly? DateCompleted { get; set; }
        public ComplianceStatus Status { get; set; }
        public bool ParentOrGuardianPresent { get; set; }
        public string? Remarks { get; set; }
    }

    public class AddCounselingDTO
    {
        [Required(ErrorMessage = "Violation is required")]
        public required string ViolationId { get; set; }

        [StringLength(200, ErrorMessage = "Intervention type cannot exceed 200 characters")]
        public string? InterventionType { get; set; }

        [StringLength(150, ErrorMessage = "Conducted by cannot exceed 150 characters")]
        public string? ConductedBy { get; set; }

        public DateOnly? ScheduledDate { get; set; }

        public bool ParentOrGuardianPresent { get; set; } = false;

        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters")]
        public string? Remarks { get; set; }
    }

    public class UpdateCounselingDTO
    {
        [Required(ErrorMessage = "CounselingRecordId is required")]
        public required string CounselingRecordId { get; set; }

        [StringLength(200, ErrorMessage = "Intervention type cannot exceed 200 characters")]
        public string? InterventionType { get; set; }

        [StringLength(150, ErrorMessage = "Conducted by cannot exceed 150 characters")]
        public string? ConductedBy { get; set; }

        public DateOnly? ScheduledDate { get; set; }

        public DateOnly? DateCompleted { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [EnumDataType(typeof(ComplianceStatus), ErrorMessage = "Invalid status")]
        public ComplianceStatus Status { get; set; }

        public bool ParentOrGuardianPresent { get; set; } = false;

        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters")]
        public string? Remarks { get; set; }
    }
}
