using System.Net;
using System.Text.Json;
using TaskBoard.Application.Common.Exceptions;

namespace TaskBoard.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
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
                _logger.LogError(ex, "An unhandled exception occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, errors) = exception switch
            {
                ValidationException ex =>
                    (HttpStatusCode.BadRequest, ex.Errors),
                NotFoundException ex =>
                    (HttpStatusCode.NotFound, new List<string> { ex.Message }),
                UnauthorizedException ex =>
                    (HttpStatusCode.Unauthorized, new List<string> { ex.Message }),
                ForbiddenException ex =>
                    (HttpStatusCode.Forbidden, new List<string> { ex.Message }),
                InvalidOperationException ex =>
                    (HttpStatusCode.Conflict, new List<string> { ex.Message }),
                _ =>
                    (HttpStatusCode.InternalServerError,
                    new List<string> { "An unexpected error occurred." })
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                errors,
                statusCode = (int)statusCode
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
    }
}
