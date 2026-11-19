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
    private readonly AppDbContext context;
    private readonly IMemoryCache cache;

    public AppSettingsService(AppDbContext context, IMemoryCache cache)
    {
        this.context = context;
        this.cache = cache;
    }

    public async Task<AppSettingsDTO> GetAsync()
    {
        // Read by every signed-in user on every page load but changed rarely: served from memory until it is updated.
        if (cache.TryGetValue(CacheKeys.AppSettings, out AppSettingsDTO? cached) && cached != null)
            return new AppSettingsDTO { ModalForms = cached.ModalForms };

        var value = await context.AppSettings
            .Where(s => s.Key == ModalFormsKey)
            .Select(s => s.Value)
            .FirstOrDefaultAsync();
        var settings = new AppSettingsDTO { ModalForms = value == "true" };
        cache.Set(CacheKeys.AppSettings, settings, CacheKeys.Expiring(CacheKeys.AppSettingsSafetyExpiry));
        return new AppSettingsDTO { ModalForms = settings.ModalForms };
    }

    public async Task<AppSettingsDTO> UpdateAsync(AppSettingsDTO settings)
    {
        var setting = await context.AppSettings.FindAsync(ModalFormsKey);
        if (setting is null)
        {
            context.AppSettings.Add(new AppSetting { Key = ModalFormsKey, Value = settings.ModalForms ? "true" : "false" });
        }
        else
        {
            setting.Value = settings.ModalForms ? "true" : "false";
        }
        await context.SaveChangesAsync();
        cache.Remove(CacheKeys.AppSettings); // the next read loads the new value
        return settings;
    }
}
