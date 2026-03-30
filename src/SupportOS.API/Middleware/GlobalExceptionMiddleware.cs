using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace SupportOS.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment env)
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
        catch (Exception ex)
        {
            var correlationId = context.TraceIdentifier;
            _logger.LogError(ex, "Unhandled exception [{CorrelationId}]: {Message}", correlationId, ex.Message);
            await WriteErrorResponseAsync(context, ex, correlationId);
        }
    }

    private async Task WriteErrorResponseAsync(HttpContext context, Exception ex, string correlationId)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title) = ex switch
        {
            ArgumentException or InvalidOperationException => (HttpStatusCode.BadRequest, "Invalid request."),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized."),
            TimeoutException or TaskCanceledException => (HttpStatusCode.GatewayTimeout, "The request timed out."),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        context.Response.StatusCode = (int)statusCode;

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = _env.IsDevelopment() ? ex.Message : "Please try again later or contact support.",
            Instance = context.Request.Path,
            Extensions = { ["correlationId"] = correlationId }
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}
