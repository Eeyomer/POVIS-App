using Microsoft.Extensions.Caching.Memory;

namespace POVISAPI.Common;

/// <summary>
/// Keys and lifetimes for the API's in-memory cache. Reference data that rarely changes is cached and is
/// cleared the moment it is edited, so users never wait for an expiry to see their own change. The expiry is
/// only a safety net (for example if the data is changed directly in the database).
/// </summary>
public static class CacheKeys
{
    /// <summary>Penalty ladder of one ordinance: cached until a penalty or section is added/removed (safety expiry 6 h).</summary>
    public static string Penalties(Guid ordinanceId) => $"penalties:{ordinanceId}";
    public static readonly TimeSpan PenaltiesSafetyExpiry = TimeSpan.FromHours(6);

    /// <summary>System settings: cached until MIS Personnel changes them (safety expiry 1 h).</summary>
    public const string AppSettings = "app-settings";
    public static readonly TimeSpan AppSettingsSafetyExpiry = TimeSpan.FromHours(1);

    public static MemoryCacheEntryOptions Expiring(TimeSpan after) =>
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(after);
}
