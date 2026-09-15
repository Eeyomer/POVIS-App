using System.Diagnostics;
using POVISAPI.Data;
using POVISAPI.Models.Entity;

namespace POVISAPI.Middleware.Logger
{
    public class LoggerMiddleware
    {
        private readonly RequestDelegate _next;
        public LoggerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext db)
        {
            var method = context.Request.Method;
            var path = context.Request.Path.ToString();
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var stopwatch = Stopwatch.StartNew();

            await _next(context);

            stopwatch.Stop();

            // By the time _next(context) returns, downstream auth middleware has
            // already populated context.User, so the acting user is available here.
            if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var username = context.User?.FindFirst("sub")?.Value;
                    db.TechnicalLogs.Add(new TechnicalLog
                    {
                        Method = method,
                        Path = path,
                        StatusCode = context.Response.StatusCode,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        Username = username,
                        IpAddress = ip
                    });
                    await db.SaveChangesAsync();
                }
                catch
                {
                    // Logging must never break the actual response.
                }
            }
        }
    }
}
