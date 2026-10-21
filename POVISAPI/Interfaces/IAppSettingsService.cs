using POVISAPI.Models.DTO;

namespace POVISAPI.Interfaces;

public interface IAppSettingsService
{
    Task<AppSettingsDTO> GetAsync();
    Task<AppSettingsDTO> UpdateAsync(AppSettingsDTO settings);
}
