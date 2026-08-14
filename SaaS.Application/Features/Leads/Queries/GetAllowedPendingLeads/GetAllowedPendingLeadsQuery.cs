using MediatR;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Models;
using System;

namespace SaaS.Application.Features.Leads.Queries.GetAllowedPendingLeads
{
    public record GetAllowedPendingLeadsQuery(Guid UserId, int BotId) : IRequest<ApiResponse<MessagingPreviewResponseDto>>;
}
