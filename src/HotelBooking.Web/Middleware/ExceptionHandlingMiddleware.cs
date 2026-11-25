using System.Text.Json;
using HotelBooking.Domain.Exceptions;

namespace HotelBooking.Web.Middleware;

public sealed class ExceptionHandlingMiddleware
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
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, StatusCodes.Status500InternalServerError, "Виникла внутрішня помилка сервера.");
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, int statusCode, string message)
    {
        var isApi = context.Request.Headers.XRequestedWith == "XMLHttpRequest"
                    || context.Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase)
                    || context.Request.Path.StartsWithSegments("/api");

        if (isApi)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";
            var payload = JsonSerializer.Serialize(new { error = message, statusCode });
            await context.Response.WriteAsync(payload);
            return;
        }

        context.Response.Redirect($"/Home/Error?message={Uri.EscapeDataString(message)}");
    }
}