using POVISAPI.Models.DTO;

namespace POVISAPI.Interfaces;

public interface ISystemMaintenanceService
{
    Task<byte[]> CreateBackupAsync();
    Task RestoreAsync(Stream backupJsonStream);
    Task<SystemHealthDTO> GetHealthAsync();
    Task<PagedResult<TechnicalLogDTO>> GetLogsAsync(TechnicalLogFilterDTO filter);
}
