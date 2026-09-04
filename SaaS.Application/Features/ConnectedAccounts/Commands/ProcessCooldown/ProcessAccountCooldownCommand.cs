using MediatR;
using SaaS.Application.Common.Models;

namespace SaaS.Application.Features.ConnectedAccounts.Commands.ProcessCooldown
{
    public record ProcessAccountCooldownCommand : IRequest<ApiResponse<int>>
    {
    }
}
