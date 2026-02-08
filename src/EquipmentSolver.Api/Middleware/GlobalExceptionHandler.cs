using System.Net;
using EquipmentSolver.Api.DTOs;

namespace EquipmentSolver.Api.Middleware;

/// <summary>
/// Catches unhandled exceptions and returns a consistent error response.
/// Hides internal details in production; exposes them in development.
/// </summary>
public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
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
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var message = _env.IsDevelopment()
                ? ex.Message
                : "An unexpected error occurred. Please try again later.";

            var response = new ErrorResponse(message);
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
