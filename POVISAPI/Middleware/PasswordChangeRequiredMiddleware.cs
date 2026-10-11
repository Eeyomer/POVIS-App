using System.Text.Json;
using POVISAPI.Common;
using POVISAPI.Models.System_Models;

namespace POVISAPI.Middleware
{
    /// <summary>
    /// Users who signed in with a temporary password may do nothing except change it.
    /// The JWT carries a "must_change_password" claim; the change-password endpoint issues a fresh token without it.
    /// </summary>
    public class PasswordChangeRequiredMiddleware
    {
        public const string ClaimType = "must_change_password";
        public const string ErrorCode = "PasswordChangeRequired";

        private readonly RequestDelegate _next;
        public PasswordChangeRequiredMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var mustChange = context.User.Identity?.IsAuthenticated == true
                && context.User.FindFirst(ClaimType)?.Value == "true";

            var path = context.Request.Path.Value ?? string.Empty;
            var allowed = path.EndsWith("/api/Auth/change-password", StringComparison.OrdinalIgnoreCase)
                || HttpMethods.IsOptions(context.Request.Method);

            if (mustChange && !allowed)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                var json = JsonSerializer.Serialize(new APIResponse<object>
                {
                    Success = false,
                    Message = "You must change your temporary password before continuing.",
                    ErrorCode = ErrorCode,
                    TimeStamp = Clock.PhNow
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                await context.Response.WriteAsync(json);
                return;
            }

            await _next(context);
        }
    }
}
