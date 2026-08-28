using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Domain.Enums;

namespace SaaS.Application.Features.Leads.Queries.GetAllLeads
{
    public class GetAllLeadsQueryHandler : IRequestHandler<GetAllLeadsQuery, ApiResponse<PaginatedResult<LeadListDto>>>
    {
        private readonly IAppDbContext _context;
        private readonly IUserBotService _userBotService;
        private readonly ILogger<GetAllLeadsQueryHandler> _logger;

        public GetAllLeadsQueryHandler(
            IAppDbContext context, 
            IUserBotService userBotService,
            ILogger<GetAllLeadsQueryHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userBotService = userBotService ?? throw new ArgumentNullException(nameof(userBotService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApiResponse<PaginatedResult<LeadListDto>>> Handle(GetAllLeadsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting GetAllLeadsQuery for UserId: {UserId}, BotId: {BotId}, Page: {PageNumber}, Size: {PageSize}", 
                request.UserId, request.BotId, request.PageNumber, request.PageSize);

            // Ownership check
            _logger.LogDebug("Checking ownership of BotId: {BotId} for UserId: {UserId}", request.BotId, request.UserId);
            var hasBot = await _userBotService.OwnerShipCheck(request.UserId, request.BotId, cancellationToken);

            if (!hasBot)
            {
                _logger.LogWarning("Ownership check failed for UserId: {UserId}, BotId: {BotId}.", request.UserId, request.BotId);
                return ApiResponse<PaginatedResult<LeadListDto>>.Failure("User or bot not found", ErrorType.NotFound);
            }

            var baseQuery = _context.Leads
                .AsNoTracking()
                .Include(l => l.Group)
                .Where(l => l.UserId == request.UserId && l.BotId == request.BotId);

            if (!string.IsNullOrWhiteSpace(request.StatusFilter))
            {
                _logger.LogDebug("Applying StatusFilter: {StatusFilter}", request.StatusFilter);
                baseQuery = baseQuery.Where(l => l.Status == request.StatusFilter);
            }

            _logger.LogDebug("Fetching total count of leads for UserId: {UserId}, BotId: {BotId}", request.UserId, request.BotId);
            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            _logger.LogDebug("Fetching page {PageNumber} of leads for UserId: {UserId}, BotId: {BotId}", request.PageNumber, request.UserId, request.BotId);
            var items = await baseQuery
                .OrderByDescending(l => l.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(l => new LeadListDto
                {
                    LeadId = l.Id,
                    LeadFormattedId = "#LED-" + l.Id,
                    ProfileName = l.ProfileName,
                    ProfileUrl = l.ProfileUrl,
                    AiMessage = l.AiMessage ?? string.Empty,
                    FromGroup = l.Group != null ? l.Group.GroupName : string.Empty,
                    Status = l.Status,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var paginated = new PaginatedResult<LeadListDto>
            {
                Items = items,
                TotalItems = totalCount,
                TotalPages = totalPages,
                CurrentPage = request.PageNumber
            };

            _logger.LogInformation("Successfully retrieved {Count} leads out of {TotalCount} total for UserId: {UserId}, BotId: {BotId}", 
                items.Count, totalCount, request.UserId, request.BotId);

            return ApiResponse<PaginatedResult<LeadListDto>>.Success(paginated, "Leads retrieved successfully");
        }
    }
}
