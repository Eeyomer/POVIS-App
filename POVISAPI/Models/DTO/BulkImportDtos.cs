namespace POVISAPI.Models.DTO
{
    public class BulkImportErrorDTO
    {
        public int Row { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class BulkImportCountDTO
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>One-time login credential generated for an imported user.</summary>
    public class BulkImportCredentialDTO
    {
        public string Username { get; set; } = string.Empty;
        public string TemporaryPassword { get; set; } = string.Empty;
    }

    public class BulkImportResultDTO
    {
        public string Module { get; set; } = string.Empty;
        public int RowsRead { get; set; }
        public List<BulkImportCountDTO> Counts { get; set; } = new();
        public List<BulkImportErrorDTO> Errors { get; set; } = new();
        /// <summary>Only filled for the Users module; shown once, never stored in readable form.</summary>
        public List<BulkImportCredentialDTO> Credentials { get; set; } = new();
    }

    public class BulkModuleInfoDTO
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }
}
