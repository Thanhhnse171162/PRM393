using CourtGo.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Middleware;

/// <summary>
/// Converts exceptions to RFC 7807 problem responses:
/// 400 validation, 403 forbidden, 404 not found, 409 conflict, 500 server error.
/// (401 is produced by the authentication middleware.)
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
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Server error")
        };

        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(ex, "Unhandled exception");

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            // Never leak internal details for 500s.
            Detail = status == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : ex.Message,
            Instance = context.Request.Path
        };

        if (ex is ValidationException v && v.Errors.Count > 0)
            problem.Extensions["errors"] = v.Errors;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }
}
