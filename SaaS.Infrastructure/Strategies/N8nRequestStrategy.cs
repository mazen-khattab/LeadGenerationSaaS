using Microsoft.Extensions.Options;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Settings;
using SaaS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace SaaS.Infrastructure.Strategies
{
    public class N8nRequestStrategy : IExternalSystemRequestStrategy
    {
        private readonly IOptionsSnapshot<N8nOptions> _options;

        public N8nRequestStrategy(IOptionsSnapshot<N8nOptions> options)
        {
            _options = options;
        }

        public ExternalSystem System => ExternalSystem.N8n;

        // n8n webhooks are sometimes passed as a full absolute URL, sometimes as a
        // relative path off the configured base URL - keep supporting both.
        public string ResolveBaseUrl(string endpoint) =>
            Uri.TryCreate(endpoint, UriKind.Absolute, out _) ? string.Empty : _options.Value.BaseUrl;

        public void ApplyAuthentication(HttpRequestMessage request)
        {
            if (!string.IsNullOrWhiteSpace(_options.Value.N8nSecret))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Value.N8nSecret);
        }

    }
}
