using MediatR;
using SaaS.Application.Common.Models;

namespace SaaS.Application.Features.TargetGroups.Commands.Delete
{
    public record DeleteGroupCommand(int Id) : IRequest<ApiResponse<bool>>;
}
