using POVISAPI.Models.Entity.Enums;
using System.ComponentModel.DataAnnotations;

namespace POVISAPI.Models.DTO
{
    public class OrdinancePenaltyDTO
    {
        public string OrdinancePenaltyId { get; set; } = string.Empty;
        public string? OrdinanceSectionId { get; set; }
        public OffenseLevel OffenseLevel { get; set; }
        public OffenderType? OffenderType { get; set; }
        public decimal? FineAmount { get; set; }
        public int? SeminarDays { get; set; }
        public int? CommunityServiceHours { get; set; }
        public int? CommunityServiceDays { get; set; }
        public bool RequiresCounseling { get; set; }
        public bool HasImprisonment { get; set; }
        public string? ImprisonmentTerm { get; set; }
        public BusinessPermitAction BusinessPermitAction { get; set; }
        public bool IsConjunctive { get; set; }
        public string PenaltyText { get; set; } = string.Empty;
    }

    public class AddOrdinancePenaltyDTO
    {
        public string? OrdinanceSectionId { get; set; }

        [Required(ErrorMessage = "Offense level is required")]
        [EnumDataType(typeof(OffenseLevel), ErrorMessage = "Invalid offense level")]
        public OffenseLevel OffenseLevel { get; set; }

        [EnumDataType(typeof(OffenderType), ErrorMessage = "Invalid offender type")]
        public OffenderType? OffenderType { get; set; }

        [Range(0, 1000000, ErrorMessage = "Fine amount must be between 0 and 1,000,000")]
        public decimal? FineAmount { get; set; }

        [Range(1, 365, ErrorMessage = "Seminar days must be between 1 and 365")]
        public int? SeminarDays { get; set; }

        [Range(1, 1000, ErrorMessage = "Community service hours must be between 1 and 1,000")]
        public int? CommunityServiceHours { get; set; }

        [Range(1, 365, ErrorMessage = "Community service days must be between 1 and 365")]
        public int? CommunityServiceDays { get; set; }

        public bool RequiresCounseling { get; set; }

        public bool HasImprisonment { get; set; }

        [StringLength(300, ErrorMessage = "Imprisonment term cannot exceed 300 characters")]
        public string? ImprisonmentTerm { get; set; }

        public BusinessPermitAction BusinessPermitAction { get; set; } = BusinessPermitAction.None;

        public bool IsConjunctive { get; set; }

        [Required(ErrorMessage = "Penalty text is required")]
        [StringLength(1000, ErrorMessage = "Penalty text cannot exceed 1,000 characters")]
        public required string PenaltyText { get; set; }
    }

    public class OrdinanceSectionDTO
    {
        public string OrdinanceSectionId { get; set; } = string.Empty;
        public string SectionCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<OrdinancePenaltyDTO> Penalties { get; set; } = new();
    }

    public class AddOrdinanceSectionDTO
    {
        [Required(ErrorMessage = "Section code is required")]
        [StringLength(50, ErrorMessage = "Section code cannot exceed 50 characters")]
        public required string SectionCode { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }
    }

    public class OrdinanceDTO
    {
        public string OrdinanceId { get; set; } = string.Empty;
        public string OrdinanceNumber { get; set; } = string.Empty;
        public int Series { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public bool IsExecutiveOrder { get; set; }
        public bool IsActive { get; set; }
        public List<OrdinanceSectionDTO> Sections { get; set; } = new();
        public List<OrdinancePenaltyDTO> Penalties { get; set; } = new();
    }

    public class OrdinanceListDTO
    {
        public string OrdinanceId { get; set; } = string.Empty;
        public string OrdinanceNumber { get; set; } = string.Empty;
        public int Series { get; set; }
        public string ShortName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class AddOrdinanceDTO
    {
        [Required(ErrorMessage = "Ordinance number is required")]
        [StringLength(20, ErrorMessage = "Ordinance number cannot exceed 20 characters")]
        public required string OrdinanceNumber { get; set; }

        [Required(ErrorMessage = "Series is required")]
        [Range(1900, 2100, ErrorMessage = "Series must be a valid year")]
        public int Series { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(500, ErrorMessage = "Title cannot exceed 500 characters")]
        public required string Title { get; set; }

        [Required(ErrorMessage = "Short name is required")]
        [StringLength(200, ErrorMessage = "Short name cannot exceed 200 characters")]
        public required string ShortName { get; set; }

        public bool IsExecutiveOrder { get; set; } = false;
    }

    public class UpdateOrdinanceDTO
    {
        [Required(ErrorMessage = "OrdinanceId is required")]
        public required string OrdinanceId { get; set; }

        [Required(ErrorMessage = "Ordinance number is required")]
        [StringLength(20, ErrorMessage = "Ordinance number cannot exceed 20 characters")]
        public required string OrdinanceNumber { get; set; }

        [Required(ErrorMessage = "Series is required")]
        [Range(1900, 2100, ErrorMessage = "Series must be a valid year")]
        public int Series { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(500, ErrorMessage = "Title cannot exceed 500 characters")]
        public required string Title { get; set; }

        [Required(ErrorMessage = "Short name is required")]
        [StringLength(200, ErrorMessage = "Short name cannot exceed 200 characters")]
        public required string ShortName { get; set; }

        public bool IsExecutiveOrder { get; set; } = false;

        public bool IsActive { get; set; } = true;
    }

    public class OrdinanceImportErrorDTO
    {
        public int Row { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class OrdinanceImportResultDTO
    {
        public int RowsRead { get; set; }
        public int OrdinancesCreated { get; set; }
        public int OrdinancesExisting { get; set; }
        public int SectionsCreated { get; set; }
        public int PenaltiesCreated { get; set; }
        public int PenaltiesSkipped { get; set; }
        public List<OrdinanceImportErrorDTO> Errors { get; set; } = new();
    }
}
