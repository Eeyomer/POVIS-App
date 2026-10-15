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

    // Parent-first order so restore can insert without violating foreign keys.
    // User accounts are intentionally excluded — they're managed via Users &
    // Roles and must not be clobbered by an operational-data restore.
    private static readonly Type[] BackupEntityTypesInOrder =
    {
        typeof(Barangay),
        typeof(Ordinance),
        typeof(OrdinanceSection),
        typeof(OrdinancePenalty),
        typeof(Violator),
        typeof(Violation),
        typeof(ViolationEvidence),
        typeof(Payment),
        typeof(FiledCase),
        typeof(SeminarRecord),
        typeof(CommunityServiceRecord),
        typeof(CounselingRecord),
    };

    public SystemMaintenanceService(AppDbContext context)
    {
        this.context = context;
    }

    public async Task<byte[]> CreateBackupAsync()
    {
        var entities = new Dictionary<string, object>();
        foreach (var type in BackupEntityTypesInOrder)
        {
            entities[type.Name] = await GetAllAsync(type);
        }

        var backup = new
        {
            GeneratedAt = DateTime.UtcNow,
            Entities = entities
        };

        return JsonSerializer.SerializeToUtf8Bytes(backup, new JsonSerializerOptions { WriteIndented = false });
    }

    public async Task RestoreAsync(Stream backupJsonStream)
    {
        using var doc = await JsonDocument.ParseAsync(backupJsonStream);
        if (!doc.RootElement.TryGetProperty("Entities", out var entitiesElement))
            throw new ArgumentException("Invalid backup file: missing 'Entities'.");

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            for (int i = BackupEntityTypesInOrder.Length - 1; i >= 0; i--)
            {
                await ClearTableAsync(BackupEntityTypesInOrder[i]);
            }

            foreach (var type in BackupEntityTypesInOrder)
            {
                if (entitiesElement.TryGetProperty(type.Name, out var arrayElement))
                {
                    await InsertAllAsync(type, arrayElement.GetRawText());
                }
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

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

    private Task<object> GetAllAsync(Type entityType)
    {
        var method = typeof(SystemMaintenanceService)
            .GetMethod(nameof(GetAllGeneric), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(entityType);
        return (Task<object>)method.Invoke(this, null)!;
    }

    private async Task<object> GetAllGeneric<T>() where T : class
    {
        return await context.Set<T>().AsNoTracking().ToListAsync();
    }

    private Task ClearTableAsync(Type entityType)
    {
        var method = typeof(SystemMaintenanceService)
            .GetMethod(nameof(ClearTableGeneric), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(entityType);
        return (Task)method.Invoke(this, null)!;
    }

    private async Task ClearTableGeneric<T>() where T : class
    {
        var all = await context.Set<T>().ToListAsync();
        if (all.Count == 0) return;
        context.Set<T>().RemoveRange(all);
        await context.SaveChangesAsync();
    }

    private Task InsertAllAsync(Type entityType, string json)
    {
        var method = typeof(SystemMaintenanceService)
            .GetMethod(nameof(InsertAllGeneric), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(entityType);
        return (Task)method.Invoke(this, new object[] { json })!;
    }

    private async Task InsertAllGeneric<T>(string json) where T : class
    {
        var items = JsonSerializer.Deserialize<List<T>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (items == null || items.Count == 0) return;

        context.Set<T>().AddRange(items);
        await context.SaveChangesAsync();
    }
}
