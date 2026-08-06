using System.ComponentModel.DataAnnotations;

namespace POVISAPI.Models.DTO
{
    public class BarangayDTO
    {
        public string BarangayId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? District { get; set; }
        public bool IsActive { get; set; }
    }

    public class AddBarangayDTO
    {
        [Required(ErrorMessage = "Barangay name is required")]
        [StringLength(100, ErrorMessage = "Barangay name cannot exceed 100 characters")]
        public required string Name { get; set; }

        [Range(1, 2, ErrorMessage = "District must be 1 or 2")]
        public int? District { get; set; }
    }

    public class UpdateBarangayDTO
    {
        [Required(ErrorMessage = "BarangayId is required")]
        public required string BarangayId { get; set; }

        [Required(ErrorMessage = "Barangay name is required")]
        [StringLength(100, ErrorMessage = "Barangay name cannot exceed 100 characters")]
        public required string Name { get; set; }

        [Range(1, 2, ErrorMessage = "District must be 1 or 2")]
        public int? District { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
