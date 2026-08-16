using System.Net;
using System.Text.Json;
using TaskTracker.Application.Exceptions;

namespace TaskTracker.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppValidationException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            var payload = new
            {
                errorCode = ex.ErrorCode,
                message = ex.Message,
                errors = ex.Errors
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, _jsonOptions));
        }
        catch (TaskItemNotFoundException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.ContentType = "application/json";
            var payload = new { errorCode = ex.ErrorCode, message = ex.Message };
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, _jsonOptions));
        }
        catch (TaskItemConflictException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Response.ContentType = "application/json";
            var payload = new { errorCode = ex.ErrorCode, message = ex.Message };
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, _jsonOptions));
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx) when (IsUniqueActiveTitleViolation(dbEx))
        {
            // Map DB unique index violation to the expected 409 with stable code
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Response.ContentType = "application/json";
            var payload = new { errorCode = "DUPLICATE_ACTIVE_TITLE", message = "An active TaskItem with this title already exists." };
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, _jsonOptions));
        }
        catch (Exception ex)
        {
            var traceId = context.TraceIdentifier;
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            var payload = new { errorCode = "UNEXPECTED_ERROR", message = "An unexpected error occurred.", traceId };
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, _jsonOptions));
        }
    }

    private static bool IsUniqueActiveTitleViolation(Microsoft.EntityFrameworkCore.DbUpdateException ex)
    {
        // SQLite constraint messages vary; check for constraint name if available or unique constraint keywords
        var msg = ex.InnerException?.Message ?? ex.Message;
        if (string.IsNullOrEmpty(msg)) return false;
        // Look for the index name (set in configuration): UX_TaskItems_Title_Active
        return msg.Contains("UX_TaskItems_Title_Active", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) && msg.Contains("TaskItems.Title", StringComparison.OrdinalIgnoreCase);
    }
}