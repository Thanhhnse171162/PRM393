using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Middleware;

/// <summary>Writes the single, consistent error body: RFC 7807 problem details + a stable "code".</summary>
public static class ApiProblem
{
    public static Task WriteAsync(
        HttpContext context, int status, string code, string title, string detail,
        IDictionary<string, string[]>? errors = null)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["code"] = code;
        if (errors is { Count: > 0 }) problem.Extensions["errors"] = errors;

        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(problem, (JsonSerializerOptions?)null, "application/problem+json");
    }
}
