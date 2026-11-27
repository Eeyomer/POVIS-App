using POVISAPI.Models.DTO;

namespace POVISAPI.Interfaces;

public interface ISystemMaintenanceService
{
    Task<List<BackupModuleDTO>> GetBackupModulesAsync();
    Task<(byte[] Content, string ModuleKey)> CreateModuleBackupAsync(string moduleKey);
    Task<int> RestoreModuleAsync(string moduleKey, Stream backupJsonStream);
    Task<SystemHealthDTO> GetHealthAsync();
    Task<PagedResult<TechnicalLogDTO>> GetLogsAsync(TechnicalLogFilterDTO filter);
}
