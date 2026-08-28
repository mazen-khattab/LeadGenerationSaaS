using System;
using System.Collections.Generic;
using MediatR;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Models;

namespace SaaS.Application.Features.Worker.Commands.DispatchMessaging
{
    public record DispatchMessagingJobCommand(Guid UserId, int BotId, int AccountId, List<long> LeadIds) : IRequest<ApiResponse<DispatchMessagingResultDto>>;
}
