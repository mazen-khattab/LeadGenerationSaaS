using MediatR;
using Microsoft.EntityFrameworkCore;
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

        public GetAllLeadsQueryHandler(IAppDbContext context, IUserBotService userBotService)
        {
            _context = context;
            _userBotService = userBotService;
        }

        public async Task<ApiResponse<PaginatedResult<LeadListDto>>> Handle(GetAllLeadsQuery request, CancellationToken cancellationToken)
        {

            // Ownership check
            var hasBot = await _userBotService.OwnerShipCheck(request.UserId, request.BotId, cancellationToken);

            if (!hasBot)
            {
                return ApiResponse<PaginatedResult<LeadListDto>>.Failure("User or bot not found", ErrorType.NotFound);
            }

            var baseQuery = _context.Leads
                .AsNoTracking()
                .Include(l => l.Group)
                .Where(l => l.UserId == request.UserId && l.BotId == request.BotId);

            if (!string.IsNullOrWhiteSpace(request.StatusFilter))
            {
                baseQuery = baseQuery.Where(l => l.Status == request.StatusFilter);
            }

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

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

            return ApiResponse<PaginatedResult<LeadListDto>>.Success(paginated, "Leads retrieved successfully");
        }
    }
}
