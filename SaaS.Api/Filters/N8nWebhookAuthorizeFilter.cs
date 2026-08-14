using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using SaaS.Application.Common.Settings;
using System.Security.Cryptography;
using System.Text;

namespace SaaS.Api.Filters
{
    public class N8nWebhookAuthorizeFilter : IAuthorizationFilter
    {
        private const string HeaderName = "X-Webhook-Secret";
        private readonly N8nOptions _options;

        public N8nWebhookAuthorizeFilter(IOptionsMonitor<N8nOptions> options)
        {
            _options = options.CurrentValue;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var headers = context.HttpContext.Request.Headers;

            // 1. Check if header exists
            if (!headers.TryGetValue(HeaderName, out var provided) || string.IsNullOrEmpty(provided))
            {
                context.Result = new JsonResult(new { message = $"Missing required header '{HeaderName}'." })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };

                return;
            }

            var n8nSecret = _options.N8nSecret;

            // 2. Check if secret is configured on the server
            if (string.IsNullOrWhiteSpace(n8nSecret))
            {
                context.Result = new JsonResult(new { message = "Server Configuration Error: N8n secret is missing." })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };

                return;
            }

            // 3. Constant-Time Comparison to prevent Timing Attacks
            var isEqual = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(provided.ToString()),
                Encoding.UTF8.GetBytes(n8nSecret)
            );

            if (!isEqual)
            {
                context.Result = new JsonResult(new { message = "Forbidden: invalid Webhook secret." })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };

                return;
            }
        }
    }
}
