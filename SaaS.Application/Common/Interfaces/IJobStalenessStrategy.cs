using Microsoft.Extensions.Logging;
using SaaS.Domain.Entities;
using SaaS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text;

namespace SaaS.Application.Common.Interfaces
{
    public interface IJobStalenessStrategy
    {
        JobType JobType { get; }

        /// <summary>
        /// Any Lead IDs this job depends on, so the caller can batch-fetch them across ALL
        /// candidate jobs in a single query instead of one query per job (fixes N+1).
        /// Return empty if this job type doesn't depend on Leads.
        /// </summary>
        IReadOnlyCollection<long> ExtractLeadIds(Job job, ILogger logger);

        /// <summary>
        /// leadIds must be the same collection previously returned by ExtractLeadIds for this job
        /// (passed back in to avoid re-parsing the payload).
        /// </summary>
        DateTime GetLastActivity(Job job, IReadOnlyCollection<long> leadIds, IReadOnlyDictionary<long, DateTime> leadProcessedAtLookup);
    }
}
