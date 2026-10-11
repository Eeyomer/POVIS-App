using POVISAPI.Models.DTO;

namespace POVISAPI.Interfaces
{
    public interface IOrdinanceImportService
    {
        /// <summary>Imports ordinances, sections and penalties from the bulk upload workbook.
        /// All-or-nothing: if any row is invalid, nothing is saved and the errors are returned.</summary>
        Task<OrdinanceImportResultDTO> ImportAsync(Stream workbook);
    }
}
