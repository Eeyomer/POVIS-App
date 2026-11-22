using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using POVISAPI.Common;
using POVISAPI.Data;
using POVISAPI.Interfaces;
using POVISAPI.Models.DTO;
using POVISAPI.Models.Entity;

namespace POVISAPI.Services;

public class AppSettingsService : IAppSettingsService
{
    private const string ModalFormsKey = "ModalForms";
    private const string IncentiveRateKey = "IncentiveRate";

    /// <summary>Rate used until MIS Personnel sets one (the fixed 10% the incentives report always used).</summary>
    public const decimal DefaultIncentiveRate = 10m;

    private readonly AppDbContext context;
    private readonly IMemoryCache cache;

    public AppSettingsService(AppDbContext context, IMemoryCache cache)
    {
        this.context = context;
        this.cache = cache;
    }

    /// <summary>Reads the current incentive percentage straight from the database (never cached, so a payment is
    /// always stamped with the rate in force at that moment). Also used by bulk import.</summary>
    public static async Task<decimal> ReadIncentiveRateAsync(AppDbContext db)
    {
        var value = await db.AppSettings.AsNoTracking()
            .Where(s => s.Key == IncentiveRateKey)
            .Select(s => s.Value)
            .FirstOrDefaultAsync();
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) && rate is >= 0 and <= 100
            ? rate
            : DefaultIncentiveRate;
    }

    public async Task<AppSettingsDTO> GetAsync()
    {
        // Read by every signed-in user on every page load but changed rarely: served from memory until it is updated.
        if (cache.TryGetValue(CacheKeys.AppSettings, out AppSettingsDTO? cached) && cached != null)
            return new AppSettingsDTO { ModalForms = cached.ModalForms, IncentiveRate = cached.IncentiveRate };

        var modalForms = await context.AppSettings
            .Where(s => s.Key == ModalFormsKey)
            .Select(s => s.Value)
            .FirstOrDefaultAsync();
        var settings = new AppSettingsDTO
        {
            ModalForms = modalForms == "true",
            IncentiveRate = await ReadIncentiveRateAsync(context)
        };
        cache.Set(CacheKeys.AppSettings, settings, CacheKeys.Expiring(CacheKeys.AppSettingsSafetyExpiry));
        return new AppSettingsDTO { ModalForms = settings.ModalForms, IncentiveRate = settings.IncentiveRate };
    }

    public async Task<AppSettingsDTO> UpdateAsync(AppSettingsDTO settings)
    {
        await UpsertAsync(ModalFormsKey, settings.ModalForms ? "true" : "false");
        await context.SaveChangesAsync();
        cache.Remove(CacheKeys.AppSettings); // the next read loads the new value
        return await GetAsync();
    }

    public Task<decimal> GetIncentiveRateAsync() => ReadIncentiveRateAsync(context);

    public async Task<decimal> UpdateIncentiveRateAsync(decimal ratePercent)
    {
        if (ratePercent < 0 || ratePercent > 100)
            throw new ArgumentException("Incentive rate must be between 0 and 100.");

        var rounded = Math.Round(ratePercent, 2);
        await UpsertAsync(IncentiveRateKey, rounded.ToString(CultureInfo.InvariantCulture));
        await context.SaveChangesAsync();
        cache.Remove(CacheKeys.AppSettings);
        return rounded;
    }

    private async Task UpsertAsync(string key, string value)
    {
        var setting = await context.AppSettings.FindAsync(key);
        if (setting is null) context.AppSettings.Add(new AppSetting { Key = key, Value = value });
        else setting.Value = value;
    }
}
