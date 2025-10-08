using System.Text.Json;
using DocumentManagementSystem.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagementSystem.Middleware;

public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorHandlingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            var pd = MapToProblemDetails(context, ex, out var status);
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = status;
            await context.Response.WriteAsync(JsonSerializer.Serialize(pd));
        }
        catch (Exception)
        {
            var pd = new ProblemDetails
            {
                Title = "Internal server error",
                Status = StatusCodes.Status500InternalServerError,
                Type = "about:blank",
                Detail = "An unexpected error occurred."
            };
            pd.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync(JsonSerializer.Serialize(pd));
        }
    }

    private static ProblemDetails MapToProblemDetails(HttpContext ctx, AppException ex, out int status)
    {
        (string type, string title, int http) = ex switch
        {
            ValidationException => ("https://httpstatuses.com/400", "Validation failed", StatusCodes.Status400BadRequest),
            NotFoundException => ("https://httpstatuses.com/404", "Resource not found", StatusCodes.Status404NotFound),
            ConflictException => ("https://httpstatuses.com/409", "Conflict", StatusCodes.Status409Conflict),
            UniqueConstraintViolationException => ("https://httpstatuses.com/409", "Unique constraint violated", StatusCodes.Status409Conflict),
            RepositoryException => ("https://httpstatuses.com/500", "Data access error", StatusCodes.Status500InternalServerError),
            _ => ("about:blank", "Error", StatusCodes.Status500InternalServerError)
        };

        status = http;

        var pd = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = http,
            Detail = ex.Detail ?? ex.Message
        };

        if (!string.IsNullOrWhiteSpace(ex.Code))
            pd.Extensions["code"] = ex.Code;

        pd.Extensions["traceId"] = ctx.TraceIdentifier;

        if (ex is ValidationException vex && vex.Errors.Any())
            pd.Extensions["errors"] = vex.Errors;

        if (ex is NotFoundException nfx)
        {
            if (!string.IsNullOrWhiteSpace(nfx.Resource))
                pd.Extensions["resource"] = nfx.Resource;
            if (nfx.ResourceId is not null)
                pd.Extensions["id"] = nfx.ResourceId;
        }

        return pd;
    }
}
