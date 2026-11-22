using POVISAPI.Models.DTO;

namespace POVISAPI.Interfaces;

public interface IAppSettingsService
{
    Task<AppSettingsDTO> GetAsync();
    Task<AppSettingsDTO> UpdateAsync(AppSettingsDTO settings);

    /// <summary>Current incentive percentage (e.g. 10 = 10%). Applies only to payments recorded from now on.</summary>
    Task<decimal> GetIncentiveRateAsync();
    Task<decimal> UpdateIncentiveRateAsync(decimal ratePercent);
}
