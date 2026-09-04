using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Application.Common.Settings;
using SaaS.Domain.Entities;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SaaS.Application.Features.Worker.Commands.ProcessJobTimeouts
{
    public class ProcessJobTimeoutsCommandHandler : IRequestHandler<ProcessJobTimeoutsCommand, ApiResponse<bool>>
    {
        private readonly IAppDbContext _dbContext;
        private readonly IAppNotificationService _notificationService;
        private readonly INetworkClient _externalSystemClient;
        private readonly ILogger<ProcessJobTimeoutsCommandHandler> _logger;
        private readonly JobWatchdogOptions _options;
        private readonly Dictionary<JobType, IJobStalenessStrategy> _strategies;

        public ProcessJobTimeoutsCommandHandler(
            IAppDbContext dbContext,
            IAppNotificationService notificationService,
            INetworkClient externalSystemClient,
            ILogger<ProcessJobTimeoutsCommandHandler> logger,
            IOptions<JobWatchdogOptions> options,
            IEnumerable<IJobStalenessStrategy> strategies)
        {
            _dbContext = dbContext;
            _notificationService = notificationService;
            _externalSystemClient = externalSystemClient;
            _logger = logger;
            _options = options.Value;
            _strategies = strategies.ToDictionary(s => s.JobType);
        }

        public async Task<ApiResponse<bool>> Handle(ProcessJobTimeoutsCommand request, CancellationToken cancellationToken)
        {
            var processingStatus = JobStatus.PROCESSING.ToDbString();

            var processingJobs = await _dbContext.Jobs
                .Where(j => j.Status == processingStatus)
                .ToListAsync(cancellationToken);

            if (processingJobs.Count == 0)
            {
                _logger.LogDebug("No jobs currently in processing state.");
                return ApiResponse<bool>.Success(true, "No jobs currently in processing state.");
            }

            _logger.LogDebug("Found {Count} jobs in processing state. Checking for timeouts.", processingJobs.Count);

            var (leadLookup, jobLeadIds) = await BuildLeadContextAsync(processingJobs, cancellationToken);

            var threshold = TimeSpan.FromMinutes(_options.TimeoutThresholdMinutes);

            foreach (var job in processingJobs)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    var leadIds = jobLeadIds.TryGetValue(job.Id, out var ids) ? ids : Array.Empty<long>();

                    await EvaluateJobAsync(job, leadIds, leadLookup, threshold, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to evaluate/update Job {JobId} during watchdog scan.", job.Id);
                }
            }

            return ApiResponse<bool>.Success(true, "Job timeouts processed successfully.");
        }

        private async Task<(Dictionary<long, DateTime> LeadLookup, Dictionary<long, IReadOnlyCollection<long>> JobLeadIds)>
            BuildLeadContextAsync(List<Job> processingJobs, CancellationToken cancellationToken)
        {
            var jobLeadIds = new Dictionary<long, IReadOnlyCollection<long>>();
            var allLeadIds = new HashSet<long>();

            foreach (var job in processingJobs)
            {
                if (!TryResolveJobType(job.Type, out var jobType) || !_strategies.TryGetValue(jobType, out var strategy))
                {
                    _logger.LogWarning("No staleness strategy found for Job {JobId} of type {JobType}", job.Id, job.Type);
                    continue;
                }

                var leadIds = strategy.ExtractLeadIds(job, _logger);
                jobLeadIds[job.Id] = leadIds;

                allLeadIds.UnionWith(leadIds);
            }

            if (allLeadIds.Count == 0)
            {
                return (new Dictionary<long, DateTime>(), jobLeadIds);
            }

            var leadLookup = await _dbContext.Leads
                .AsNoTracking()
                .Where(l => allLeadIds.Contains(l.Id) && l.ProcessedAt != null)
                .Select(l => new { l.Id, ProcessedAt = l.ProcessedAt!.Value })
                .ToDictionaryAsync(l => l.Id, l => l.ProcessedAt, cancellationToken);

            _logger.LogDebug("Built context for {JobCount} jobs. Resolved {LeadCount} total leads.", processingJobs.Count, allLeadIds.Count);

            return (leadLookup, jobLeadIds);
        }

        private async Task EvaluateJobAsync(Job job, IReadOnlyCollection<long> leadIds, Dictionary<long, DateTime> leadLookup, TimeSpan threshold, CancellationToken cancellationToken)
        {
            var lastActivity = job.CreatedAt;

            if (TryResolveJobType(job.Type, out var jobType) && _strategies.TryGetValue(jobType, out var strategy))
            {
                lastActivity = strategy.GetLastActivity(job, leadIds, leadLookup);
            }

            if (DateTime.UtcNow - lastActivity <= threshold)
            {
                _logger.LogDebug("Job {JobId} is not stale yet. Last activity: {LastActivity}", job.Id, lastActivity);
                return; // Not stale yet.
            }

            _logger.LogWarning("Job {JobId} (Type: {Type}) looks stale (last activity {LastActivity}). Verifying with the worker before failing it.", job.Id, job.Type, lastActivity);

            var isWorkerAlive = await IsWorkerStillProcessingAsync(job, cancellationToken);

            if (isWorkerAlive)
            {
                _logger.LogInformation("Job {JobId} confirmed still active on the worker. Skipping this cycle.", job.Id);
                return;
            }

            _logger.LogWarning("Job {JobId} confirmed unreachable/crashed. Marking as FAILED.", job.Id);

            var rowsAffected = await TryMarkJobFailedAsync(job, cancellationToken);

            if (rowsAffected == 0)
            {
                _logger.LogInformation("Job {JobId} was updated by another process before the watchdog could write. Skipping notification.", job.Id);
                return;
            }

            await _notificationService.NotifyJobFailedAsync(job.UserId, job.Id, "Something went wrong, please try again");
        }

        private async Task<bool> IsWorkerStillProcessingAsync(Job job, CancellationToken cancellationToken)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.LivenessCheckTimeoutSeconds));

                var endpoint = $"jobs/status?job_id={job.Id}";
                var response = await _externalSystemClient.GetAsync(endpoint, ExternalSystem.NodeWorker, timeoutCts.Token);

                if (response is null || !response.IsSuccess)
                {
                    return false;
                }

                var body = response.Content;
                var status = JsonSerializer.Deserialize<NodeWorkerJobStatusResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return string.Equals(status?.Status, "processing", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(status?.Status, "running", StringComparison.OrdinalIgnoreCase);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Liveness check for Job {JobId} timed out. Treating worker as unreachable.", job.Id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Liveness check for Job {JobId} failed. Treating worker as unreachable.", job.Id);
                return false;
            }
        }

        private async Task<int> TryMarkJobFailedAsync(Job job, CancellationToken cancellationToken)
        {
            job.Status = JobStatus.FAILED.ToDbString();

            try 
            {
                var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(job.PayloadJson);
                if (payload != null && payload.TryGetValue("accountId", out var accountIdObj) && int.TryParse(accountIdObj.ToString(), out int accountId))
                {
                    var account = await _dbContext.ConnectedAccounts.FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
                    if (account != null && account.Status == AccountStatus.BUSY.ToDbString())
                    {
                        account.Status = AccountStatus.ACTIVE.ToDbString();
                        account.LastStatusUpdatedAt = DateTime.UtcNow;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update ConnectedAccount status for Job {JobId} during watchdog scan", job.Id);
            }

            try
            {
                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while marking Job {JobId} as FAILED.", job.Id);
                return 0;
            }
        }

        private static bool TryResolveJobType(string typeValue, out JobType jobType) => Enum.TryParse(typeValue, ignoreCase: true, out jobType);

        private sealed class NodeWorkerJobStatusResponse
        {
            public string? Status { get; set; }
        }
    }
}
