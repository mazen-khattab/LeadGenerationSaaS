using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaaS.Application.Common.Interfaces;
using SaaS.Domain.Entities;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;
using System.Text.Json;

namespace SaaS.Infrastructure.Services
{
    public class JobTimeoutWatchdogService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<JobTimeoutWatchdogService> _logger;
        private readonly JobWatchdogOptions _options;

        public JobTimeoutWatchdogService(IServiceScopeFactory scopeFactory, ILogger<JobTimeoutWatchdogService> logger, IOptions<JobWatchdogOptions> options)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromMinutes(_options.CheckIntervalMinutes);

            _logger.LogInformation("JobTimeoutWatchdogService is starting. CheckInterval={Interval}m, TimeoutThreshold={Threshold}m", 
                _options.CheckIntervalMinutes, _options.TimeoutThresholdMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckForStuckJobsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing JobTimeoutWatchdogService.");
                }

                await Task.Delay(interval, stoppingToken);
            }

            _logger.LogInformation("JobTimeoutWatchdogService is stopping.");
        }

        private async Task CheckForStuckJobsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<IAppNotificationService>();
            var externalSystemClient = scope.ServiceProvider.GetRequiredService<INetworkClient>();

            // return something like: 
            /*  {
				  JobType.MESSAGING : MessagingJobStalenessStrategy_Instance,
				}
			 */
            var strategies = scope.ServiceProvider.GetServices<IJobStalenessStrategy>()
                .ToDictionary(s => s.JobType);

            var processingStatus = JobStatus.PROCESSING.ToDbString();

            var processingJobs = await dbContext.Jobs
                .Where(j => j.Status == processingStatus)
                .ToListAsync(cancellationToken);

            if (processingJobs.Count == 0)
            {
                _logger.LogDebug("No jobs currently in processing state.");
                return;
            }

            _logger.LogDebug("Found {Count} jobs in processing state. Checking for timeouts.", processingJobs.Count);

            var (leadLookup, jobLeadIds) = await BuildLeadContextAsync(dbContext, processingJobs, strategies, cancellationToken);

            var threshold = TimeSpan.FromMinutes(_options.TimeoutThresholdMinutes);

            foreach (var job in processingJobs)
            {
                // Graceful Shutdown Check:
                // If the server receives a stop/shutdown signal, exit the loop immediately.
                // This prevents the app from getting force-killed while processing a huge batch,
                // allowing current work to finish safely and quickly.
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    var leadIds = jobLeadIds.TryGetValue(job.Id, out var ids) ? ids : Array.Empty<long>();

                    await EvaluateJobAsync(
                        job, strategies, leadIds, leadLookup, threshold,
                        dbContext, notificationService, externalSystemClient, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Error Isolation:
                    // Wrap each job in a try/catch so a failure in one job doesn't crash 
                    // the entire loop or prevent remaining jobs from being evaluated.
                    _logger.LogError(ex, "Failed to evaluate/update Job {JobId} during watchdog scan.", job.Id);
                }
            }
        }

        // ✅
        private async Task<(Dictionary<long, DateTime> LeadLookup, Dictionary<long, IReadOnlyCollection<long>> JobLeadIds)>
            BuildLeadContextAsync(IAppDbContext dbContext, List<Job> processingJobs, Dictionary<JobType, IJobStalenessStrategy> strategies, CancellationToken cancellationToken)
        {
            var jobLeadIds = new Dictionary<long, IReadOnlyCollection<long>>();
            var allLeadIds = new HashSet<long>();

            foreach (var job in processingJobs)
            {
                if (!TryResolveJobType(job.Type, out var jobType) || !strategies.TryGetValue(jobType, out var strategy))
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

            var leadLookup = await dbContext.Leads
                .AsNoTracking()
                .Where(l => allLeadIds.Contains(l.Id) && l.ProcessedAt != null)
                .Select(l => new { l.Id, ProcessedAt = l.ProcessedAt!.Value })
                .ToDictionaryAsync(l => l.Id, l => l.ProcessedAt, cancellationToken);

            _logger.LogDebug("Built context for {JobCount} jobs. Resolved {LeadCount} total leads.", processingJobs.Count, allLeadIds.Count);

            return (leadLookup, jobLeadIds);
        }

        private async Task EvaluateJobAsync(Job job, Dictionary<JobType, IJobStalenessStrategy> strategies, IReadOnlyCollection<long> leadIds, Dictionary<long, DateTime> leadLookup, TimeSpan threshold, IAppDbContext dbContext, IAppNotificationService notificationService, INetworkClient externalSystemClient, CancellationToken cancellationToken)
        {
            var lastActivity = job.CreatedAt;

            if (TryResolveJobType(job.Type, out var jobType) && strategies.TryGetValue(jobType, out var strategy))
            {
                lastActivity = strategy.GetLastActivity(job, leadIds, leadLookup);
            }

            if (DateTime.UtcNow - lastActivity <= threshold)
            {
                _logger.LogDebug("Job {JobId} is not stale yet. Last activity: {LastActivity}", job.Id, lastActivity);
                return; // Not stale yet.
            }

            _logger.LogWarning("Job {JobId} (Type: {Type}) looks stale (last activity {LastActivity}). Verifying with the worker before failing it.", job.Id, job.Type, lastActivity);

            var isWorkerAlive = await IsWorkerStillProcessingAsync(job, externalSystemClient, cancellationToken);

            if (isWorkerAlive)
            {
                _logger.LogInformation("Job {JobId} confirmed still active on the worker. Skipping this cycle.", job.Id);
                return;
            }

            _logger.LogWarning("Job {JobId} confirmed unreachable/crashed. Marking as FAILED.", job.Id);

            var rowsAffected = await TryMarkJobFailedAsync(dbContext, job, cancellationToken);

            if (rowsAffected == 0)
            {
                _logger.LogInformation("Job {JobId} was updated by another process before the watchdog could write. Skipping notification.", job.Id);
                return;
            }

            await notificationService.NotifyJobFailedAsync(job.UserId, job.Id, "Something went wrong, please try again");
        }

        // ✅
        private async Task<bool> IsWorkerStillProcessingAsync(Job job, INetworkClient externalSystemClient, CancellationToken cancellationToken)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.LivenessCheckTimeoutSeconds));

                var endpoint = $"jobs/status?job_id={job.Id}";
                var response = await externalSystemClient.GetAsync(endpoint, ExternalSystem.NodeWorker, timeoutCts.Token);

                if (response is null || !response.IsSuccess)
                {
                    // No response / non-success => can't confirm liveness => treat as crashed.
                    return false;
                }

                var body = response.Content;
                var status = JsonSerializer.Deserialize<NodeWorkerJobStatusResponse>(body);

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

        // ✅
        private async Task<int> TryMarkJobFailedAsync(IAppDbContext dbContext, Job job, CancellationToken cancellationToken)
        {
            job.Status = JobStatus.FAILED.ToDbString();

            try 
            {
                var payload = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(job.PayloadJson);
                if (payload != null && payload.TryGetValue("accountId", out var accountIdObj) && int.TryParse(accountIdObj.ToString(), out int accountId))
                {
                    var account = await dbContext.ConnectedAccounts.FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
                    if (account != null && account.Status == AccountStatus.BUSY.ToDbString())
                    {
                        account.Status = AccountStatus.ACTIVE.ToDbString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update ConnectedAccount status for Job {JobId} during watchdog scan", job.Id);
            }

            try
            {
                return await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while marking Job {JobId} as FAILED.", job.Id);
                return 0;
            }
        }

        // ✅
        private static bool TryResolveJobType(string typeValue, out JobType jobType) => Enum.TryParse(typeValue, ignoreCase: true, out jobType);

        // ✅
        private sealed class NodeWorkerJobStatusResponse
        {
            public string? Status { get; set; }
        }
    }
}
