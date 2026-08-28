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
        private readonly IOptionsSnapshot<WorkerOptions> _options;

        public NodeWorkerRequestStrategy(IOptionsSnapshot<WorkerOptions> options)
        {
            _options = options;
        }

        public ExternalSystem System => ExternalSystem.NodeWorker;

        public string ResolveBaseUrl(string endpoint) => _options.Value.BaseUrl;

        public void ApplyAuthentication(HttpRequestMessage request)
        {
            if (!string.IsNullOrWhiteSpace(_options.Value.WorkerSecret))
                request.Headers.Add("X-Worker-Api-Key", _options.Value.WorkerSecret);
        }

    }
}
