using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Application.Common.Settings;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SaaS.Application.Features.Leads.Queries.GetAllowedPendingLeads
{
    public class GetAllowedPendingLeadsQueryHandler : IRequestHandler<GetAllowedPendingLeadsQuery, ApiResponse<MessagingPreviewResponseDto>>
    {
        private readonly IAppDbContext _context;
        private readonly IUserBotService _userBotService;
        private readonly IOptionsSnapshot<GeneralSettings> _options;
        private readonly ILogger<GetAllowedPendingLeadsQueryHandler> _logger;

        public GetAllowedPendingLeadsQueryHandler(
            IAppDbContext context, 
            IUserBotService userBotService, 
            IOptionsSnapshot<GeneralSettings> options, 
            ILogger<GetAllowedPendingLeadsQueryHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userBotService = userBotService ?? throw new ArgumentNullException(nameof(userBotService));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApiResponse<MessagingPreviewResponseDto>> Handle(GetAllowedPendingLeadsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting allowed pending leads query. UserId: {UserId}, BotId: {BotId}", request.UserId, request.BotId);

            var settings = _options.Value;

            // Ownership check
            _logger.LogDebug("Validating bot ownership for UserId: {UserId}, BotId: {BotId}", request.UserId, request.BotId);
            var owns = await _userBotService.OwnerShipCheck(request.UserId, request.BotId, cancellationToken);
            if (!owns)
            {
                _logger.LogWarning("Ownership check failed. User {UserId} does not own Bot {BotId}", request.UserId, request.BotId);
                return ApiResponse<MessagingPreviewResponseDto>.Failure("User or bot not found", ErrorType.NotFound);
            }
            _logger.LogDebug("Bot ownership confirmed. UserId: {UserId}, BotId: {BotId}", request.UserId, request.BotId);

            // Fetch daily limit (fallback to default 50 if not configured)
            _logger.LogDebug("Fetching daily message limit from UserSettings for UserId: {UserId}", request.UserId);
            var dailyLimit = await _context.UserSettings
                .AsNoTracking()
                .Where(us => us.UserId == request.UserId)
                .Select(us => (int?)us.DailyMessageLimit)
                .FirstOrDefaultAsync(cancellationToken) ?? 50;

            _logger.LogDebug("Daily message limit resolved: {DailyLimit} for UserId: {UserId}", dailyLimit, request.UserId);

            var windowStart = DateTime.UtcNow.AddHours(-settings.DailyLimitResetHour);
            _logger.LogDebug("Querying sent messages since {WindowStart} (Reset interval: {ResetHours} hours) for UserId: {UserId}, BotId: {BotId}", 
                windowStart, settings.DailyLimitResetHour, request.UserId, request.BotId);

            var messagesSent = await _context.Leads
                .AsNoTracking()
                .Where(l => l.UserId == request.UserId && l.BotId == request.BotId && l.Status == LeadStatus.COMPLETED.ToDbString() && l.ProcessedAt >= windowStart)
                .CountAsync(cancellationToken);

            var remaining = Math.Max(0, dailyLimit - messagesSent);
            _logger.LogInformation("Daily messaging stats - Sent: {MessagesSent}/{DailyLimit}, Remaining allowed: {Remaining} for UserId: {UserId}, BotId: {BotId}", 
                messagesSent, dailyLimit, remaining, request.UserId, request.BotId);

            var leads = new List<MessagingPreviewLeadDto>();

            if (remaining > 0)
            {
                _logger.LogDebug("Fetching up to {Remaining} pending leads for UserId: {UserId}, BotId: {BotId}", remaining, request.UserId, request.BotId);

                leads = await _context.Leads
                    .AsNoTracking()
                    .Where(l => l.UserId == request.UserId && l.BotId == request.BotId && l.Status == LeadStatus.PENDING.ToDbString())
                    .OrderBy(l => l.CreatedAt)
                    .Take(remaining)
                    .Select(l => new MessagingPreviewLeadDto
                    {
                        LeadId = l.Id,
                        LeadNumber = "#LED-" + l.Id,
                        ProfileName = l.ProfileName,
                        ProfileUrl = l.ProfileUrl,
                        AiMessage = l.AiMessage ?? string.Empty,
                        Status = l.Status
                    })
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Fetched {Count} pending leads for preview. UserId: {UserId}, BotId: {BotId}", leads.Count, request.UserId, request.BotId);
            }
            else
            {
                _logger.LogInformation("Daily message limit reached ({MessagesSent}/{DailyLimit}). Skipping pending leads fetch for UserId: {UserId}, BotId: {BotId}", 
                    messagesSent, dailyLimit, request.UserId, request.BotId);
            }

            var response = new MessagingPreviewResponseDto
            {
                DailyMessageLimit = dailyLimit,
                MessagesSent = messagesSent,
                AllowedToProcessCount = remaining,
                Leads = leads
            };

            _logger.LogInformation("Successfully generated messaging preview for UserId: {UserId}, BotId: {BotId}", request.UserId, request.BotId);
            return ApiResponse<MessagingPreviewResponseDto>.Success(response, "Messaging preview retrieved successfully");
        }
    }
}
