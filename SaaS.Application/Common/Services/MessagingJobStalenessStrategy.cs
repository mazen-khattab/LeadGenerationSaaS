using Microsoft.Extensions.Logging;
using SaaS.Application.Common.Interfaces;
using SaaS.Domain.Entities;
using SaaS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text;
using System.Text.Json;

namespace SaaS.Application.Common.Services
{
    public sealed class MessagingJobStalenessStrategy : IJobStalenessStrategy
    {
        public JobType JobType => JobType.MESSAGING;

        public IReadOnlyCollection<long> ExtractLeadIds(Job job, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(job.PayloadJson))
            {
                return Array.Empty<long>();
            }

            var payload = JsonSerializer.Deserialize<JsonElement>(job.PayloadJson);
            if (payload.TryGetProperty("leadIds", out var leadIdsProp) && leadIdsProp.ValueKind == JsonValueKind.Array)
            {
                return leadIdsProp.EnumerateArray().Select(x => x.GetInt64()).ToList();
            }

            return Array.Empty<long>();
        }

        public DateTime GetLastActivity(Job job, IReadOnlyCollection<long> leadIds, IReadOnlyDictionary<long, DateTime> leadProcessedAtLookup)
        {
            DateTime? latest = null;

            foreach (var id in leadIds)
            {
                if (leadProcessedAtLookup.TryGetValue(id, out var processedAt))
                {
                    if (latest is null || processedAt > latest)
                    {
                        latest = processedAt;
                    }
                }
            }

            return latest.HasValue && latest.Value > job.CreatedAt ? latest.Value : job.CreatedAt;
        }
    }

}
