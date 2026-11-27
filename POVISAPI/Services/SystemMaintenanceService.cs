using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using POVISAPI.Data;
using POVISAPI.Interfaces;
using POVISAPI.Models.DTO;
using POVISAPI.Models.Entity;

namespace POVISAPI.Services;

public class SystemMaintenanceService : ISystemMaintenanceService
{
    private readonly AppDbContext context;

    private sealed record BackupModule(string Key, string Name, string Description, Type[] EntityTypes);

    // Modules are listed parent-first, and entity types inside each module are
    // parent-first too, so a module restore never inserts a child before its parent.
    // User accounts are intentionally excluded — they're managed via Users & Roles.
    private static readonly BackupModule[] Modules =
    {
        new("barangays", "Barangays", "Barangay reference list.",
            new[] { typeof(Barangay) }),
        new("ordinances", "Ordinances", "Ordinances with their sections and penalties.",
            new[] { typeof(Ordinance), typeof(OrdinanceSection), typeof(OrdinancePenalty) }),
        new("violators", "Violators", "Violator profiles.",
            new[] { typeof(Violator) }),
        new("violations", "Violations", "Violation records and their evidence.",
            new[] { typeof(Violation), typeof(ViolationEvidence) }),
        new("payments", "Payments", "Payment records.",
            new[] { typeof(Payment) }),
        new("filed-cases", "Filed Cases", "Cases filed in court.",
            new[] { typeof(FiledCase) }),
        new("compliance", "Compliance Records", "Seminar, community service, and counseling records.",
            new[] { typeof(SeminarRecord), typeof(CommunityServiceRecord), typeof(CounselingRecord) }),
    };

    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };

    public SystemMaintenanceService(AppDbContext context)
    {
        this.context = context;
    }

    public async Task<List<BackupModuleDTO>> GetBackupModulesAsync()
    {
        var result = new List<BackupModuleDTO>();
        foreach (var module in Modules)
        {
            var count = 0;
            foreach (var type in module.EntityTypes)
                count += await CountAsync(type);

            result.Add(new BackupModuleDTO
            {
                Key = module.Key,
                Name = module.Name,
                Description = module.Description,
                RecordCount = count
            });
        }
        return result;
    }

    public async Task<(byte[] Content, string ModuleKey)> CreateModuleBackupAsync(string moduleKey)
    {
        var module = FindModule(moduleKey);

        var entities = new Dictionary<string, object>();
        foreach (var type in module.EntityTypes)
            entities[type.Name] = await GetAllAsync(type);

        var backup = new
        {
            Module = module.Key,
            GeneratedAt = DateTime.UtcNow,
            Entities = entities
        };

        return (JsonSerializer.SerializeToUtf8Bytes(backup), module.Key);
    }

    /// <summary>
    /// Restores one module from a backup file. Records in the file are re-created
    /// or overwritten with their backed-up values; records added after the backup
    /// are kept, so records in other modules that point to them are never broken.
    /// Also accepts an older full-system backup file — only this module's tables are read.
    /// </summary>
    public async Task<int> RestoreModuleAsync(string moduleKey, Stream backupJsonStream)
    {
        var module = FindModule(moduleKey);

        JsonDocument doc;
        try
        {
            doc = await JsonDocument.ParseAsync(backupJsonStream);
        }
        catch (JsonException)
        {
            throw new ArgumentException("Invalid backup file: not valid JSON.");
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("Entities", out var entitiesElement))
                throw new ArgumentException("Invalid backup file: missing 'Entities'.");

            if (root.TryGetProperty("Module", out var moduleElement) &&
                moduleElement.ValueKind == JsonValueKind.String &&
                !string.Equals(moduleElement.GetString(), module.Key, StringComparison.OrdinalIgnoreCase))
            {
                var fileKey = moduleElement.GetString();
                var fileModule = Modules.FirstOrDefault(m => m.Key == fileKey)?.Name ?? fileKey;
                throw new ArgumentException($"This backup file is for the {fileModule} module, not {module.Name}.");
            }

            if (!module.EntityTypes.Any(t => entitiesElement.TryGetProperty(t.Name, out _)))
                throw new ArgumentException($"The backup file has no {module.Name} data.");

            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var restored = 0;
                foreach (var type in module.EntityTypes)
                {
                    if (entitiesElement.TryGetProperty(type.Name, out var arrayElement))
                        restored += await UpsertAllAsync(type, arrayElement.GetRawText());
                }

                await transaction.CommitAsync();
                return restored;
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                context.ChangeTracker.Clear();
                throw new InvalidOperationException(
                    $"Could not restore {module.Name}: some records point to data that no longer exists " +
                    "or conflict with existing records. Restore the modules they depend on first " +
                    "(Barangays, Ordinances, Violators, then Violations) and try again.");
            }
            catch
            {
                await transaction.RollbackAsync();
                context.ChangeTracker.Clear();
                throw;
            }
        }
    }

    private static BackupModule FindModule(string moduleKey) =>
        Modules.FirstOrDefault(m => string.Equals(m.Key, moduleKey, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"Unknown backup module '{moduleKey}'.");

    public async Task<SystemHealthDTO> GetHealthAsync()
    {
        bool connected;
        try
        {
            connected = await context.Database.CanConnectAsync();
        }
        catch
        {
            connected = false;
        }

        return new SystemHealthDTO
        {
            DatabaseConnected = connected,
            ServerTimeUtc = DateTime.UtcNow,
            TotalUsers = await context.Users.CountAsync(),
            TotalViolators = await context.Violators.CountAsync(),
            TotalViolations = await context.Violations.CountAsync(),
            TotalTechnicalLogs = await context.TechnicalLogs.CountAsync()
        };
    }

    public async Task<PagedResult<TechnicalLogDTO>> GetLogsAsync(TechnicalLogFilterDTO filter)
    {
        var query = context.TechnicalLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Method))
            query = query.Where(l => l.Method == filter.Method);
        if (filter.StatusCode.HasValue)
            query = query.Where(l => l.StatusCode == filter.StatusCode);
        if (filter.From.HasValue)
            query = query.Where(l => l.Timestamp >= filter.From);
        if (filter.To.HasValue)
            query = query.Where(l => l.Timestamp <= filter.To);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(l =>
                EF.Functions.ILike(l.Path, $"%{term}%") ||
                (l.Username != null && EF.Functions.ILike(l.Username, $"%{term}%")));
        }

        var total = await query.CountAsync();
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 200);

        var entities = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return new PagedResult<TechnicalLogDTO>
        {
            Items = entities.Select(l => new TechnicalLogDTO
            {
                Id = l.Id.ToString(),
                Timestamp = l.Timestamp,
                Method = l.Method,
                Path = l.Path,
                StatusCode = l.StatusCode,
                DurationMs = l.DurationMs,
                Username = l.Username,
                IpAddress = l.IpAddress
            }).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private Task<object> GetAllAsync(Type entityType) =>
        (Task<object>)InvokeGeneric(nameof(GetAllGeneric), entityType);

    // Archived rows are included so a restore brings them back too.
    private async Task<object> GetAllGeneric<T>() where T : BaseEntity =>
        await context.Set<T>().IgnoreQueryFilters().AsNoTracking().ToListAsync();

    private Task<int> CountAsync(Type entityType) =>
        (Task<int>)InvokeGeneric(nameof(CountGeneric), entityType);

    private Task<int> CountGeneric<T>() where T : BaseEntity =>
        context.Set<T>().IgnoreQueryFilters().CountAsync();

    private Task<int> UpsertAllAsync(Type entityType, string json) =>
        (Task<int>)InvokeGeneric(nameof(UpsertAllGeneric), entityType, json);

    private async Task<int> UpsertAllGeneric<T>(string json) where T : BaseEntity
    {
        var items = JsonSerializer.Deserialize<List<T>>(json, ReadOptions);
        if (items == null || items.Count == 0) return 0;

        var set = context.Set<T>();
        var existing = await set.IgnoreQueryFilters().ToDictionaryAsync(e => e.Id);

        foreach (var item in items)
        {
            if (existing.TryGetValue(item.Id, out var current))
                context.Entry(current).CurrentValues.SetValues(item);
            else
                set.Add(item);
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return items.Count;
    }

    private object InvokeGeneric(string methodName, Type entityType, params object[] args) =>
        typeof(SystemMaintenanceService)
            .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(entityType)
            .Invoke(this, args.Length == 0 ? null : args)!;
}
