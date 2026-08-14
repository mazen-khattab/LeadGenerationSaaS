using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Application.Common.Settings;
using SaaS.Domain.Enums;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SaaS.Application.Features.Leads.Queries.GetAllowedPendingLeads
{
    public class GetAllowedPendingLeadsQueryHandler : IRequestHandler<GetAllowedPendingLeadsQuery, ApiResponse<MessagingPreviewResponseDto>>
    {
        private readonly IAppDbContext _context;
        private readonly IUserBotService _userBotService;
        private readonly GeneralSettings _generalSettings;

        public GetAllowedPendingLeadsQueryHandler(IAppDbContext context, IUserBotService userBotService, IOptionsMonitor<GeneralSettings> options)
        {
            _context = context;
            _userBotService = userBotService;
            _generalSettings = options.CurrentValue;
        }

        public async Task<ApiResponse<MessagingPreviewResponseDto>> Handle(GetAllowedPendingLeadsQuery request, CancellationToken cancellationToken)
        {
            // Ownership check
            var owns = await _userBotService.OwnerShipCheck(request.UserId, request.BotId, cancellationToken);
            if (!owns)
            {
                return ApiResponse<MessagingPreviewResponseDto>.Failure("User or bot not found", ErrorType.NotFound);
            }

            // Fetch daily limit (fallback to default 30 if not present)
            var dailyLimit = await _context.UserSettings
                .AsNoTracking()
                .Where(us => us.UserId == request.UserId)
                .Select(us => (int?)us.DailyMessageLimit)
                .FirstOrDefaultAsync(cancellationToken) ?? 50;

            var windowStart = DateTime.UtcNow.AddHours(_generalSettings.DailyLimitResetHour);

            var messagesSent = await _context.Leads
                .AsNoTracking()
                .Where(l => l.UserId == request.UserId && l.BotId == request.BotId && l.Status == "Completed" && l.ProcessedAt >= windowStart)
                .CountAsync(cancellationToken);

            var remaining = Math.Max(0, dailyLimit - messagesSent);

            var leads = new List<MessagingPreviewLeadDto>();

            if (remaining > 0)
            {
                leads = await _context.Leads
                    .AsNoTracking()
                    .Where(l => l.UserId == request.UserId && l.BotId == request.BotId && l.Status == "Pending")
                    .OrderBy(l => l.CreatedAt)
                    .Take(remaining)
                    .Select(l => new MessagingPreviewLeadDto
                    {
                        LeadId = l.Id,
                        LeadFormattedId = "#LED-" + l.Id,
                        ProfileName = l.ProfileName,
                        ProfileUrl = l.ProfileUrl,
                        AiMessage = l.AiMessage ?? string.Empty,
                        Status = l.Status
                    })
                    .ToListAsync(cancellationToken);
            }

            var response = new MessagingPreviewResponseDto
            {
                DailyMessageLimit = dailyLimit,
                MessagesSent = messagesSent,
                AllowedToProcessCount = remaining,
                Leads = leads
            };

            return ApiResponse<MessagingPreviewResponseDto>.Success(response, "Messaging preview retrieved successfully");
        }
    }
}
