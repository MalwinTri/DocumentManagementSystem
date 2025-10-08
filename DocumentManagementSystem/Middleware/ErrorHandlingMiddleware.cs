using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using DocumentManagementSystem.Exceptions;

namespace DocumentManagementSystem.Middleware;

public sealed class ErrorHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BadHttpRequestException ex)
        {
            await WriteProblem(context, MapBadRequest(context, ex), StatusCodes.Status400BadRequest, ex);
        }
        catch (OperationCanceledException ex) when (context.RequestAborted.IsCancellationRequested)
        {
            await WriteProblem(context, MapClientClosed(context, ex), 499, ex);
        }
        catch (AppException ex)
        {
            var pd = MapAppException(context, ex, out var status);
            await WriteProblem(context, pd, status, ex);
        }
        catch (Exception ex)
        {
            var pd = new ProblemDetails
            {
                Title = "Internal server error",
                Status = StatusCodes.Status500InternalServerError,
                Type = "about:blank",
                Detail = _env.IsDevelopment() ? ex.ToString() : "An unexpected error occurred.",
                Instance = context.Request.Path
            };
            pd.Extensions["traceId"] = context.TraceIdentifier;

            await WriteProblem(context, pd, StatusCodes.Status500InternalServerError, ex, logAsError: true);
        }
    }

    private async Task WriteProblem(HttpContext ctx, ProblemDetails pd, int statusCode, Exception ex, bool logAsError = false)
    {
        var level = logAsError || statusCode >= 500 ? LogLevel.Error :
                    statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

        _logger.Log(level, ex, "HTTP {Status} {Title}. Path={Path} TraceId={TraceId}",
            statusCode, pd.Title, ctx.Request.Path, ctx.TraceIdentifier);

        if (ctx.Response.HasStarted)
        {
            _logger.LogWarning("Response already started. Cannot write problem details.");
            return;
        }

        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "application/problem+json";

        if (HttpMethods.IsHead(ctx.Request.Method))
            return;

        await ctx.Response.WriteAsync(JsonSerializer.Serialize(pd, JsonOpts));
    }

    private static ProblemDetails MapAppException(HttpContext ctx, AppException ex, out int status)
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
            Detail = ex.Detail ?? ex.Message,
            Instance = ctx.Request.Path
        };

        pd.Extensions["traceId"] = ctx.TraceIdentifier;

        if (!string.IsNullOrWhiteSpace(ex.Code))
            pd.Extensions["code"] = ex.Code;

        if (ex is ValidationException vex && vex.Errors?.Any() == true)
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

    private static ProblemDetails MapBadRequest(HttpContext ctx, BadHttpRequestException ex) =>
        new()
        {
            Type = "https://httpstatuses.com/400",
            Title = "Bad request",
            Status = StatusCodes.Status400BadRequest,
            Detail = ex.Message,
            Instance = ctx.Request.Path,
            Extensions = { ["traceId"] = ctx.TraceIdentifier }
        };

    private static ProblemDetails MapClientClosed(HttpContext ctx, OperationCanceledException ex) =>
        new()
        {
            Type = "about:blank",
            Title = "Client closed request",
            Status = 499, 
            Detail = "The request was aborted by the client.",
            Instance = ctx.Request.Path,
            Extensions = { ["traceId"] = ctx.TraceIdentifier }
        };
}
