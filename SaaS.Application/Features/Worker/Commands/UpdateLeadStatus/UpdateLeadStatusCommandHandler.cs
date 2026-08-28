using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SaaS.Application.Features.Worker.Commands.UpdateLeadStatus
{
    public class UpdateLeadStatusCommandHandler : IRequestHandler<UpdateLeadStatusCommand, ApiResponse<bool>>
    {
        private readonly IAppDbContext _context;
        private readonly IAppNotificationService _notificationService;
        private readonly ILogger<UpdateLeadStatusCommandHandler> _logger;

        public UpdateLeadStatusCommandHandler(
            IAppDbContext context,
            IAppNotificationService notificationService,
            ILogger<UpdateLeadStatusCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApiResponse<bool>> Handle(UpdateLeadStatusCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating Lead {LeadId} status to {Status}", request.LeadId, request.Status);

            var lead = await _context.Leads
                .FirstOrDefaultAsync(l => l.Id == request.LeadId, cancellationToken);

            if (lead == null)
            {
                _logger.LogWarning("Lead {LeadId} not found.", request.LeadId);
                return ApiResponse<bool>.Failure("Lead not found.", ErrorType.NotFound);
            }

            if (Enum.TryParse<LeadStatus>(request.Status, true, out var leadStatusEnum))
            {
                lead.Status = leadStatusEnum.ToDbString();
            }
            else
            {
                lead.Status = request.Status; // Fallback, though validator should prevent this
            }
            
            // Set ProcessedAt if the status is a final state
            if (string.Equals(request.Status, LeadStatus.COMPLETED.ToDbString(), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(request.Status, LeadStatus.FAILED.ToDbString(), StringComparison.OrdinalIgnoreCase))
            {
                lead.ProcessedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Lead {LeadId} updated successfully. Sending notification to UserId: {UserId}", lead.Id, lead.UserId);

            // Notify UI real-time
            await _notificationService.NotifyLeadStatusUpdatedAsync(lead.UserId, lead.Id, lead.Status);

            return ApiResponse<bool>.Success(true, "Lead status updated successfully.");
        }
    }
}
