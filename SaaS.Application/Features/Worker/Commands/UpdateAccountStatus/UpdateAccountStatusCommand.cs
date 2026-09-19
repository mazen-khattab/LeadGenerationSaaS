using MediatR;
using SaaS.Application.Common.Models;

namespace SaaS.Application.Features.Worker.Commands.UpdateAccountStatus
{
    public record UpdateAccountStatusCommand(int AccountId, string Status) : IRequest<ApiResponse<bool>>;
}
