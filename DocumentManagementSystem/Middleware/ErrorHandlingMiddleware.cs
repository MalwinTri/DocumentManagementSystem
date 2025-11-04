using DocumentManagementSystem.DAL.Postgres.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
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
        // häufige Spezialfälle VOR dem generischen Catch
        catch (BadHttpRequestException ex)
        {
            await WriteProblem(context, MapBadRequest(context, ex), StatusCodes.Status400BadRequest, ex);
        }
        catch (OperationCanceledException ex) when (context.RequestAborted.IsCancellationRequested)
        {
            // 499 = Client Closed Request (nginx), ASP.NET hat keinen StatusCode-Const dafür
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
            GetExtensions(pd)["traceId"] = context.TraceIdentifier;

            await WriteProblem(context, pd, StatusCodes.Status500InternalServerError, ex, logAsError: true);
        }
    }

    private async Task WriteProblem(HttpContext ctx, ProblemDetails pd, int statusCode, Exception ex, bool logAsError = false)
    {
        // Logging
        var level = logAsError || statusCode >= 500 ? LogLevel.Error :
                    statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

        _logger.Log(level, ex, "HTTP {Status} {Title}. Path={Path} TraceId={TraceId}",
            statusCode, pd.Title, ctx.Request.Path, ctx.TraceIdentifier);

        // Wenn bereits gestartet: nichts mehr schreiben
        if (ctx.Response.HasStarted)
        {
            _logger.LogWarning("Response already started. Cannot write problem details.");
            return;
        }

        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "application/problem+json";

        // HEAD-Requests: keinen Body
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

        GetExtensions(pd)["traceId"] = ctx.TraceIdentifier;

        if (!string.IsNullOrWhiteSpace(ex.Code))
            GetExtensions(pd)["code"] = ex.Code;

        if (ex is ValidationException vex && vex.Errors?.Any() == true)
            GetExtensions(pd)["errors"] = vex.Errors;

        if (ex is NotFoundException nfx)
        {
            if (!string.IsNullOrWhiteSpace(nfx.Resource))
                GetExtensions(pd)["resource"] = nfx.Resource;
            if (nfx.ResourceId is not null)
                GetExtensions(pd)["id"] = nfx.ResourceId;
        }

        return pd;
    }

    private static ProblemDetails MapBadRequest(HttpContext ctx, BadHttpRequestException ex)
        {
        var pd = new ProblemDetails
        {
            Type = "https://httpstatuses.com/400",
            Title = "Bad request",
            Status = StatusCodes.Status400BadRequest,
            Detail = ex.Message,
            Instance = ctx.Request.Path
        };
        GetExtensions(pd)["traceId"] = ctx.TraceIdentifier;
        return pd;
    }

    private static ProblemDetails MapClientClosed(HttpContext ctx, OperationCanceledException ex)
        {
        var pd = new ProblemDetails
        {
            Type = "about:blank",
            Title = "Client closed request",
            Status = 499, // custom
            Detail = "The request was aborted by the client.",
            Instance = ctx.Request.Path
        };
        GetExtensions(pd)["traceId"] = ctx.TraceIdentifier;
        return pd;
    }

    // Add this helper method to allow extensions on ProblemDetails
    private static IDictionary<string, object> GetExtensions(ProblemDetails pd)
    {
        // Use a backing dictionary via reflection or create a new one if not present
        // For simplicity, attach a dictionary via a property bag (Items) on ProblemDetails
        // If you control ProblemDetails, consider adding an Extensions property directly
        const string key = "__extensions";
        if (pd is not null)
        {
            if (pd is IDictionary<string, object> dict)
                return dict;
            var itemsProp = pd.GetType().GetProperty("Items");
            if (itemsProp != null)
            {
                var items = itemsProp.GetValue(pd) as IDictionary<object, object>;
                if (items != null)
                {
                    if (!items.ContainsKey(key))
                        items[key] = new Dictionary<string, object>();
                    return (Dictionary<string, object>)items[key];
                }
            }
        }
        // fallback: create a new dictionary (not persisted)
        return new Dictionary<string, object>();
    }
}
