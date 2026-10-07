using CourtGo.Application.Common.Exceptions;

namespace CourtGo.Api.Middleware;

/// <summary>
/// Converts exceptions to RFC 7807 problem responses with a stable "code":
/// 400 validation, 401, 403, 404, 409, 500. Stack traces are never returned.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (status, title) = ex switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Server error")
        };

        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(ex, "Unhandled exception");

        context.Response.Clear();

        var code = ex is AppException app ? app.Code : ErrorCodes.InternalError;
        // Never leak internal details for 500s.
        var detail = status == StatusCodes.Status500InternalServerError ? "An unexpected error occurred." : ex.Message;
        var errors = ex is ValidationException v ? v.Errors : null;

        await ApiProblem.WriteAsync(context, status, code, title, detail, errors);
    }
}
