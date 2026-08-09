using Microsoft.EntityFrameworkCore;
using SaaS.Application.Common.Models;
using SaaS.Domain.ExceptionTypes;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _env;
        private const string LogDirectory = "my-logs";
        private const string LogFileName = "logs.txt";

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
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
                _logger.LogError(ex, "Unhandled exception occurred. Path: {Path}, Method: {Method}", context.Request.Path, context.Request.Method);

                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("The response has already started; the error response cannot be written.");
                    return;
                }

                var errorResponse = BuildErrorResponse(context, ex);

                await WriteErrorToFileAsync(errorResponse, ex);
                await WriteErrorResponseAsync(context, errorResponse);
            }
        }

        private ApiErrorResponse BuildErrorResponse(HttpContext context, Exception exception)
        {
            Console.WriteLine(exception.Message);
            var (statusCode, message) = exception switch
            {
                NotFoundException => (HttpStatusCode.NotFound, exception.Message),
                ValidationException => (HttpStatusCode.BadRequest, exception.Message),
                UnauthorizedException => (HttpStatusCode.Unauthorized, "Unauthorized access"),
                DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "The record was modified by another user. Please reload and try again."),
                _ => (HttpStatusCode.InternalServerError, "An internal server error occurred")
            };

            Console.WriteLine(statusCode);
            Console.WriteLine(message);

            return new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = (int)statusCode,
                Message = message,
                Path = context.Request.Path.Value ?? string.Empty,
                Method = context.Request.Method,
                TraceId = Activity.Current?.Id ?? context.TraceIdentifier,
                Details = _env.IsDevelopment() ? exception.StackTrace : null,
                InnerException = _env.IsDevelopment() ? exception.InnerException?.Message : null
            };
        }

        private static Task WriteErrorResponseAsync(HttpContext context, ApiErrorResponse errorResponse)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = errorResponse.StatusCode;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var errorJson = JsonSerializer.Serialize(errorResponse, options);
            return context.Response.WriteAsync(errorJson);
        }

        private async Task WriteErrorToFileAsync(ApiErrorResponse error, Exception exception)
        {
            try
            {
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), LogDirectory);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var filePath = Path.Combine(folderPath, LogFileName);

                var sb = new StringBuilder();
                sb.AppendLine(new string('-', 50));
                sb.AppendLine($"Timestamp : {DateTime.UtcNow:O}");
                sb.AppendLine($"Status    : {error.StatusCode}");
                sb.AppendLine($"Message   : {error.Message}");
                sb.AppendLine($"Path      : {error.Path}");
                sb.AppendLine($"Method    : {error.Method}");
                sb.AppendLine($"TraceId   : {error.TraceId}");
                sb.AppendLine("StackTrace:");
                sb.AppendLine(exception.StackTrace);
                if (exception.InnerException != null)
                {
                    sb.AppendLine("InnerEx   :");
                    sb.AppendLine(exception.InnerException.ToString());
                }
                sb.AppendLine();

                await File.AppendAllTextAsync(filePath, sb.ToString());
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "Failure to write error to log file.");
            }
        }
    }
}