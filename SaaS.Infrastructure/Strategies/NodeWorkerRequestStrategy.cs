using Microsoft.Extensions.Options;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Settings;
using SaaS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Infrastructure.Strategies
{
    public class NodeWorkerRequestStrategy : IExternalSystemRequestStrategy
    {
        private readonly WorkerOptions _options;

        public NodeWorkerRequestStrategy(IOptionsMonitor<WorkerOptions> options)
        {
            _options = options.CurrentValue;
        }

        public ExternalSystem System => ExternalSystem.NodeWorker;

        public string ResolveBaseUrl(string endpoint) => _options.BaseUrl;

        public void ApplyAuthentication(HttpRequestMessage request)
        {
            if (!string.IsNullOrWhiteSpace(_options.WorkerSecret))
                request.Headers.Add("X-Worker-Api-Key", _options.WorkerSecret);
        }

    }
}
