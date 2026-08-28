using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Domain.Entities;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;

namespace SaaS.Application.Features.Runs.Commands.Complete
{
    public class CompleteRunCommandHandler : IRequestHandler<CompleteRunCommand, ApiResponse<bool>>
    {
        private readonly IAppDbContext _context;
        private readonly IAppNotificationService _notificationService;
        private readonly ILogger<CompleteRunCommandHandler> _logger;

        public CompleteRunCommandHandler(
            IAppDbContext context, 
            IAppNotificationService notificationService,
            ILogger<CompleteRunCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApiResponse<bool>> Handle(CompleteRunCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting completion process for RunId {RunId} with {LeadCount} incoming leads", request.RunId, request.Leads?.Count ?? 0);

            _logger.LogDebug("Fetching run from database for RunId: {RunId}", request.RunId);
            var run = await _context.Runs
                .FirstOrDefaultAsync(r => r.Id == request.RunId, cancellationToken);

            if (run is null)
            {
                _logger.LogWarning("Run not found for RunId: {RunId}", request.RunId);
                return ApiResponse<bool>.Failure("Run not found.", ErrorType.NotFound);
            }

            if (!string.Equals(run.Status, RunStatus.RUNNING.ToDbString(), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Cannot complete RunId {RunId}. Current status is '{Status}', expected '{ExpectedStatus}'", run.Id, run.Status, RunStatus.RUNNING.ToDbString());
                return ApiResponse<bool>.Failure("Run is not in progress or has already been finalized.", ErrorType.ValidationError);
            }

            var newLeads = new List<Lead>();

            // Process leads only if there are any
            if (request.Leads.Any())
            {
                var incomingIds = request.Leads
                    .Select(x => x.ExternalId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct()
                    .ToList();

                if (incomingIds.Any())
                {
                    _logger.LogDebug("Checking database for {IncomingCount} incoming lead IDs for UserId: {UserId}", incomingIds.Count, run.UserId);
                    var existingFromDb = await _context.Leads
                        .AsNoTracking()
                        .Where(l => l.UserId == run.UserId && incomingIds.Contains(l.ExternalId))
                        .Select(l => l.ExternalId)
                        .ToListAsync(cancellationToken);

                    _logger.LogDebug("Found {ExistingCount} existing leads out of {IncomingCount} incoming IDs", existingFromDb.Count, incomingIds.Count);
                    var existingIds = new HashSet<string>(existingFromDb, StringComparer.OrdinalIgnoreCase);

                    var newUniqueLeadsDto = request.Leads
                        .Where(l => !string.IsNullOrWhiteSpace(l.ExternalId) && !existingIds.Contains(l.ExternalId));

                    newLeads = newUniqueLeadsDto.Select(dto => new Lead
                    {
                        UserId = run.UserId,
                        BotId = run.BotId,
                        ExternalId = dto.ExternalId!,
                        GroupId = run.GroupId,
                        AccountId = run.AccountId,
                        RunId = run.Id,
                        ProfileName = dto.Username ?? string.Empty,
                        ProfileUrl = dto.ExternalId!,
                        AiMessage = dto.AiMessage ?? string.Empty,
                        Status = LeadStatus.PENDING.ToDbString(),
                        CreatedAt = DateTime.UtcNow,
                        Detail = new LeadDetail { MetaDataJson = string.IsNullOrWhiteSpace(dto.MetadataJson) ? "{}" : dto.MetadataJson }
                    }).ToList();

                    if (newLeads.Any())
                    {
                        _logger.LogInformation("Persisting {NewLeadsCount} new unique leads for RunId {RunId}", newLeads.Count, run.Id);
                        await _context.Leads.AddRangeAsync(newLeads, cancellationToken);
                    }
                    else
                    {
                        _logger.LogInformation("All incoming leads were duplicates. No new leads to persist for RunId {RunId}", run.Id);
                    }
                }
                else
                {
                    _logger.LogDebug("No valid ExternalIds found in incoming leads for RunId {RunId}", run.Id);
                }
            }
            else
            {
                _logger.LogInformation("No incoming leads provided for RunId {RunId}", run.Id);
            }

            // Single Path: Finalize run
            _logger.LogDebug("Updating run status to COMPLETED and saving changes for RunId: {RunId}", run.Id);
            run.Status = RunStatus.COMPLETED.ToDbString();
            run.CollectedLeadsCount = newLeads.Count;
            run.EndedAt = DateTime.UtcNow;

            if (run.AccountId.HasValue)
            {
                var account = await _context.ConnectedAccounts.FirstOrDefaultAsync(a => a.Id == run.AccountId, cancellationToken);
                if (account != null && account.Status == AccountStatus.BUSY.ToDbString())
                {
                    account.Status = AccountStatus.COOLING_DOWN.ToDbString();
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Run {RunId} successfully marked as completed with {CollectedLeadsCount} new leads", run.Id, run.CollectedLeadsCount);

            // Single Path: Notify
            try
            {
                _logger.LogDebug("Sending completion notification for RunId: {RunId}", run.Id);
                await _notificationService.NotifyRunCompletedAsync(run.UserId, run.Id, newLeads.Count);
                _logger.LogInformation("Successfully sent completion notification for RunId: {RunId}", run.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send run completion notification for RunId {RunId}", run.Id);
            }

            return ApiResponse<bool>.Success(true, 
                newLeads.Any() ? "Run completed and unique leads persisted." : "Run completed with no leads.");
        }
    }
}
