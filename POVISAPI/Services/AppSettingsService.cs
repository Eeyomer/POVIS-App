using Microsoft.EntityFrameworkCore;
using POVISAPI.Data;
using POVISAPI.Interfaces;
using POVISAPI.Models.DTO;
using POVISAPI.Models.Entity;

namespace POVISAPI.Services;

public class AppSettingsService : IAppSettingsService
{
    private const string ModalFormsKey = "ModalForms";
    private readonly AppDbContext context;

    public AppSettingsService(AppDbContext context)
    {
        this.context = context;
    }

    public async Task<AppSettingsDTO> GetAsync()
    {
        var value = await context.AppSettings
            .Where(s => s.Key == ModalFormsKey)
            .Select(s => s.Value)
            .FirstOrDefaultAsync();
        return new AppSettingsDTO { ModalForms = value == "true" };
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
        return settings;
    }
}
