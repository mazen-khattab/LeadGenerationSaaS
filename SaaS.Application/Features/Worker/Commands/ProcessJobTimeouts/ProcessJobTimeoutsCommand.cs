using MediatR;
using SaaS.Application.Common.Models;

namespace SaaS.Application.Features.Worker.Commands.ProcessJobTimeouts
{
    public class ProcessJobTimeoutsCommand : IRequest<ApiResponse<bool>>
    {
    }
}
