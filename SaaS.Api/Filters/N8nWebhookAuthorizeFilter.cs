using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SaaS.Application.Common.Settings;
using Microsoft.Extensions.Options;

namespace SaaS.Api.Filters
{
    public class N8nWebhookAuthorizeFilter : IAsyncActionFilter
    {
        private readonly N8nSecurity _n8nSecurity;

        public N8nWebhookAuthorizeFilter(IOptions<N8nSecurity> options)
        {
            _n8nSecurity = options.Value;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var headers = context.HttpContext.Request.Headers;
            var secret = _n8nSecurity.AuthSecret;

            if (!headers.TryGetValue("X-Webhook-Secret", out var provided) || string.IsNullOrWhiteSpace(secret) || provided != secret)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            await next();
        }
    }
}
