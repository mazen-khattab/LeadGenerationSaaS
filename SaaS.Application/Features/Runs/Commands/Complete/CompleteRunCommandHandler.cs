using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Domain.Entities;
using SaaS.Domain.Enums;

namespace SaaS.Application.Features.Runs.Commands.Complete
{
    public class CompleteRunCommandHandler : IRequestHandler<CompleteRunCommand, ApiResponse<bool>>
    {
        private readonly IAppDbContext _context;
        private readonly IAppNotificationService _notificationService;

        public CompleteRunCommandHandler(IAppDbContext context, IAppNotificationService notificationService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        public async Task<ApiResponse<bool>> Handle(CompleteRunCommand request, CancellationToken cancellationToken)
        {
            // 1. Fetch run
            var run = await _context.Runs
                .FirstOrDefaultAsync(r => r.Id == request.RunId, cancellationToken);

            if (run is null)
                return ApiResponse<bool>.Failure("Run not found.", ErrorType.NotFound);

            // 2. Ensure in-progress
            if (!string.Equals(run.Status, RunStatus.RUNNING.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<bool>.Failure("Run is not in progress or has already been finalized.", ErrorType.ValidationError);
            }

            // If there are no incoming leads, finalize immediately with zero new leads
            if (!request.Leads.Any())
            {
                run.Status = RunStatus.COMPLETED.ToString();
                run.CollectedLeadsCount = 0;
                run.EndedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                try
                {
                    await _notificationService.NotifyRunCompletedAsync(run.UserId, run.Id, 0);
                }
                catch
                {
                    // Swallow notification failures
                }

                return ApiResponse<bool>.Success(true, "Run completed with no leads.");
            }

            // 3. Extract incoming ExternalIds
            var incomingIds = request.Leads
                .Select(x => x.ExternalId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            // 4. High-performance exists query: only select ProfileUrl
            HashSet<string> existingIds = new(StringComparer.OrdinalIgnoreCase);
            if (incomingIds.Any())
            {
                var existingFromDb = await _context.Leads
                    .AsNoTracking()
                    .Where(l => l.UserId == run.UserId && incomingIds.Contains(l.ExternalId))
                    .Select(l => l.ExternalId)
                    .ToListAsync(cancellationToken);

                existingIds.UnionWith(existingFromDb);
            }
            
            // 5. Filter incoming leads to only new ones
            var newUniqueLeadsDto = request.Leads
                .Where(l => !string.IsNullOrWhiteSpace(l.ExternalId) && !existingIds.Contains(l.ExternalId))
                .ToList();

            var newLeads = new List<Lead>();
            if (newUniqueLeadsDto.Any())
            {
                foreach (var dto in newUniqueLeadsDto)
                {
                    var lead = new Lead
                    {
                        UserId = run.UserId,
                        BotId = run.BotId,
                        ExternalId = dto.ExternalId ?? string.Empty,
                        GroupId = run.GroupId,
                        AccountId = run.AccountId,
                        RunId = run.Id,
                        ProfileName = dto.Username ?? string.Empty,
                        ProfileUrl = dto.ExternalId ?? string.Empty,
                        AiMessage = dto.AiMessage ?? string.Empty,
                        Status = LeadStatus.PINDING.ToString(),
                        CreatedAt = DateTime.UtcNow,
                        Detail = new LeadDetail { MetaDataJson = string.IsNullOrWhiteSpace(dto.MetadataJson) ? "{}" : dto.MetadataJson }
                    };

                    newLeads.Add(lead);
                }

                await _context.Leads.AddRangeAsync(newLeads, cancellationToken);
            }

            // 6. Finalize run
            run.Status = RunStatus.COMPLETED.ToString();
            run.CollectedLeadsCount = newLeads.Count;
            run.EndedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // 7. Notify
            try
            {
                await _notificationService.NotifyRunCompletedAsync(run.UserId, run.Id, newLeads.Count);
            }
            catch
            {
                // Non-blocking
            }

            return ApiResponse<bool>.Success(true, "Run completed and unique leads persisted.");
        }
    }
}
