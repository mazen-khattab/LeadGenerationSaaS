using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using SaaS.Application.Common.Settings;
using System.Security.Cryptography;
using System.Text;

namespace SaaS.Api.Filters
{
    public class BotWorkerAuthorizeFilter : IAsyncAuthorizationFilter
    {
        private const string HeaderName = "x-worker-api-key";
        private readonly IOptionsSnapshot<WorkerOptions> _options;

        public BotWorkerAuthorizeFilter(IOptionsSnapshot<WorkerOptions> options)
        {
            _options = options;
        }

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var headers = context.HttpContext.Request.Headers;
            var workerOptions = _options.Value;

            if (!headers.TryGetValue(HeaderName, out var provided) || string.IsNullOrEmpty(provided))
            {
                context.Result = new JsonResult(new { message = $"Missing required header '{HeaderName}'." })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };

                return Task.CompletedTask;
            }

            var workerSecret = workerOptions.WorkerSecret;

            if (string.IsNullOrWhiteSpace(workerSecret))
            {
                context.Result = new JsonResult(new { message = "Server Configuration Error: Worker secret is missing." })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
                return Task.CompletedTask;
            }

            var isEqual = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(provided.ToString()),
                Encoding.UTF8.GetBytes(workerSecret)
            );

            if (!isEqual)
            {
                context.Result = new JsonResult(new { message = "Forbidden: invalid API key." })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
                return Task.CompletedTask;
            }

            // Authorized - continue pipeline
            return Task.CompletedTask;
        }
    }
}
