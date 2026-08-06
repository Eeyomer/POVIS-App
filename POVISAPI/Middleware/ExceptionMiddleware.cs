using POVISAPI.Common;
using POVISAPI.Middleware.Exceptions;
using POVISAPI.Models.System_Models;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Authentication;
using System.Text.Json;

namespace POVISAPI.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }

            // 400 — Validation errors (must be before Exception catch-all)
            catch (ValidationException ex)
            {
                _logger.LogWarning("Validation failed: {Errors}", ex.Errors);
                await WriteResponse(context, HttpStatusCode.BadRequest, ex.Message, ex.Errors);
            }

            // 404 — Not Found
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found: {Message}", ex.Message);
                await WriteResponse(context, HttpStatusCode.NotFound, ex.Message);
            }

            // 400 — Bad Request / Business Rule Violations
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
                await WriteResponse(context, HttpStatusCode.BadRequest, ex.Message);
            }

            // 400 — Invalid Arguments
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument: {Message}", ex.Message);
                await WriteResponse(context, HttpStatusCode.BadRequest, ex.Message);
            }

            // 403 — Forbidden
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access: {Message}", ex.Message);
                await WriteResponse(context, HttpStatusCode.Forbidden, ex.Message);
            }

            // 401 — Unauthenticated
            catch (AuthenticationException ex)
            {
                _logger.LogWarning(ex, "Authentication failed: {Message}", ex.Message);
                await WriteResponse(context, HttpStatusCode.Unauthorized, ex.Message);
            }

            // 400 — EF Core DB write failure
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error: {Message}", ex.Message);
                await WriteResponse(context, HttpStatusCode.BadRequest,
                    "A database error occurred. Please check your data and try again.");
            }

            // 499 — Client cancelled the request
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "Request was cancelled.");
                await WriteResponse(context, HttpStatusCode.BadRequest, "Request was cancelled.");
            }

            // 500 — Catch-all (always last)
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                await WriteResponse(context, HttpStatusCode.InternalServerError,
                    "An unexpected error occurred. Please try again later.");
            }
        }

        // ✅ Added optional data parameter
        private static async Task WriteResponse(
            HttpContext context,
            HttpStatusCode statusCode,
            string message,
            object? data = null)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = new APIResponse<object>
            {
                Success = false,
                Message = message,
                Data = data,
                ErrorCode = statusCode.ToString(),
                TimeStamp = Clock.PhNow
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}