using System.Collections.Generic;
using MediatR;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Models;

namespace SaaS.Application.Features.Runs.Commands.Complete
{
    public record CompleteRunCommand(int RunId, List<ScrapedLeadDto> Leads) : IRequest<ApiResponse<bool>>;
}
