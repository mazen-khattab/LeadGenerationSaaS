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
            _logger.LogInformation("Starting job timeout processing.");
            var processingStatus = JobStatus.PROCESSING.ToDbString();

            _logger.LogInformation("Fetching jobs with status {Status} from the database.", processingStatus);
            var processingJobs = await _dbContext.Jobs
                .Where(j => j.Status == processingStatus)
                .ToListAsync(cancellationToken);

            if (processingJobs.Count == 0)
            {
                _logger.LogInformation("No jobs currently in processing state. Exiting watchdog scan.");
                _logger.LogDebug("No jobs currently in processing state.");
                return ApiResponse<bool>.Success(true, "No jobs currently in processing state.");
            }

            _logger.LogInformation("Found {Count} jobs in processing state. Checking for timeouts.", processingJobs.Count);
            _logger.LogDebug("Found {Count} jobs in processing state. Checking for timeouts.", processingJobs.Count);

            var (leadLookup, jobLeadIds) = await BuildLeadContextAsync(processingJobs, cancellationToken);

            _logger.LogInformation("Lead context built. Evaluating each job for staleness.");
            var threshold = TimeSpan.FromMinutes(_options.TimeoutThresholdMinutes);

            _logger.LogInformation("Timeout threshold set to {ThresholdMinutes} minutes.", _options.TimeoutThresholdMinutes);
            foreach (var job in processingJobs)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Cancellation requested. Exiting job timeout processing.");
                    break;
                }

                try
                {
                    _logger.LogInformation("Evaluating Job {JobId} of type {JobType}.", job.Id, job.Type);
                    var leadIds = jobLeadIds.TryGetValue(job.Id, out var ids) ? ids : Array.Empty<long>();

                    await EvaluateJobAsync(job, leadIds, leadLookup, threshold, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to evaluate/update Job {JobId} during watchdog scan.", job.Id);
                }
            }

            _logger.LogInformation("Job timeout processing completed.");
            return ApiResponse<bool>.Success(true, "Job timeouts processed successfully.");
        }

        private async Task<(Dictionary<long, DateTime> LeadLookup, Dictionary<long, IReadOnlyCollection<long>> JobLeadIds)>
            BuildLeadContextAsync(List<Job> processingJobs, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Building lead context for {JobCount} jobs.", processingJobs.Count);
            var jobLeadIds = new Dictionary<long, IReadOnlyCollection<long>>();
            var allLeadIds = new HashSet<long>();

            foreach (var job in processingJobs)
            {
                _logger.LogInformation("Processing Job {JobId} of type {JobType}.", job.Id, job.Type);
                if (!TryResolveJobType(job.Type, out var jobType) || !_strategies.TryGetValue(jobType, out var strategy))
                {
                    _logger.LogWarning("No staleness strategy found for Job {JobId} of type {JobType}", job.Id, job.Type);
                    continue;
                }

                _logger.LogInformation("Extracting lead IDs for Job {JobId} using strategy for type {JobType}.", job.Id, job.Type);
                var leadIds = strategy.ExtractLeadIds(job, _logger);
                jobLeadIds[job.Id] = leadIds;

                allLeadIds.UnionWith(leadIds);
            }

            _logger.LogInformation("Total unique lead IDs to resolve: {LeadCount}", allLeadIds.Count);
            if (allLeadIds.Count == 0)
            {
                _logger.LogInformation("No lead IDs found for any jobs. Skipping lead resolution.");
                return (new Dictionary<long, DateTime>(), jobLeadIds);
            }

            _logger.LogInformation("Resolving lead processed timestamps for {LeadCount} leads.", allLeadIds.Count);
            var leadLookup = await _dbContext.Leads
                .AsNoTracking()
                .Where(l => allLeadIds.Contains(l.Id) && l.ProcessedAt != null)
                .Select(l => new { l.Id, ProcessedAt = l.ProcessedAt!.Value })
                .ToDictionaryAsync(l => l.Id, l => l.ProcessedAt, cancellationToken);

            _logger.LogInformation("Built context for {JobCount} jobs. Resolved {LeadCount} total leads.", processingJobs.Count, allLeadIds.Count);
            _logger.LogDebug("Built context for {JobCount} jobs. Resolved {LeadCount} total leads.", processingJobs.Count, allLeadIds.Count);

            return (leadLookup, jobLeadIds);
        }

        private async Task EvaluateJobAsync(Job job, IReadOnlyCollection<long> leadIds, Dictionary<long, DateTime> leadLookup, TimeSpan threshold, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Evaluating Job {JobId} of type {JobType} for staleness.", job.Id, job.Type);
            var lastActivity = job.CreatedAt;

            _logger.LogDebug("Initial last activity for Job {JobId} set to CreatedAt: {CreatedAt}", job.Id, lastActivity);
            if (TryResolveJobType(job.Type, out var jobType) && _strategies.TryGetValue(jobType, out var strategy))
            {
                _logger.LogInformation("Using staleness strategy for Job {JobId} of type {JobType} to determine last activity.", job.Id, job.Type);
                lastActivity = strategy.GetLastActivity(job, leadIds, leadLookup);
            }

            _logger.LogInformation("Last activity for Job {JobId} determined to be: {LastActivity}", job.Id, lastActivity);
            if (DateTime.UtcNow - lastActivity <= threshold)
            {
                _logger.LogInformation("Job {JobId} is not stale yet. Last activity: {LastActivity}", job.Id, lastActivity);
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
            _logger.LogInformation("Checking if worker is still processing Job {JobId}.", job.Id);
            try
            {
                _logger.LogDebug("Sending liveness check request for Job {JobId} to external system.", job.Id);
                _logger.LogInformation("Liveness check timeout is set to {TimeoutSeconds} seconds.", _options.LivenessCheckTimeoutSeconds);
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.LivenessCheckTimeoutSeconds));

                var endpoint = $"jobs/{job.Id}/status";
                var response = await _externalSystemClient.GetAsync(endpoint, ExternalSystem.NodeWorker, timeoutCts.Token);

                if (response is null || !response.IsSuccess)
                {
                    _logger.LogWarning("Liveness check for Job {JobId} failed with status code {StatusCode}. Treating worker as unreachable.", job.Id, response?.StatusCode);
                    return false;
                }

                _logger.LogInformation("Liveness check for Job {JobId} succeeded. Parsing response.", job.Id);
                var body = response.Content;
                var status = JsonSerializer.Deserialize<NodeWorkerJobStatusResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return string.Equals(status?.Status, JobStatus.PROCESSING.ToDbString(), StringComparison.OrdinalIgnoreCase);
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
            _logger.LogInformation("Marking Job {JobId} as FAILED in the database.", job.Id);
            job.Status = JobStatus.FAILED.ToDbString();

            try 
            {
                _logger.LogInformation("Attempting to update ConnectedAccount status for Job {JobId}.", job.Id);
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
