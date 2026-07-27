using System.Net;
using DataFlow.Business.Common;
using DataFlow.Business.Dtos.Common;

namespace DataFlow.API.Middleware;

/// <summary>
/// Tek merkezden hata yönetimi. Controller'larda try/catch tekrarı olmaz ve
/// istemciye her zaman aynı biçimde (ApiResponse) hata döner.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
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
            var (status, message) = ex switch
            {
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, ex.Message),
                InvalidOperationException => (HttpStatusCode.Conflict, ex.Message),
                InvalidDataException => (HttpStatusCode.BadRequest, ex.Message),
                NotSupportedException => (HttpStatusCode.UnsupportedMediaType, ex.Message),
                KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message),
                _ => (HttpStatusCode.InternalServerError, "Beklenmeyen bir hata oluştu.")
            };

            if (status == HttpStatusCode.InternalServerError)
                _logger.LogError(ex, "İşlenmemiş hata: {Path}", context.Request.Path);
            else
                _logger.LogWarning("{Status} — {Message}", (int)status, ex.Message);

            context.Response.Clear();
            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/json; charset=utf-8";

            // Yığın izi yalnızca geliştirme ortamında sızdırılır.
            var details = _env.IsDevelopment()
                ? new[] { ex.ToString() }
                : Array.Empty<string>();

            var body = ApiResponse<object>.Fail(message, details);

            await context.Response.WriteAsync(JsonDefaults.Serialize(body));
        }
    }
}
