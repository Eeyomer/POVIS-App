using POVISAPI.Models.DTO;

namespace POVISAPI.Interfaces
{
    public interface IBulkImportService
    {
        List<BulkModuleInfoDTO> ListModules();

        /// <summary>The .xlsx template (Instructions, data sheet, Example, Lists) for a module.</summary>
        byte[] GetTemplate(string module);

        /// <summary>All-or-nothing import: if any row is invalid nothing is saved and the errors are returned.</summary>
        Task<BulkImportResultDTO> ImportAsync(string module, Stream workbook, string uploader);
    }
}
