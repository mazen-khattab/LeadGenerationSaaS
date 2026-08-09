using System;
using MediatR;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Models;

namespace SaaS.Application.Features.Runs.Commands.Create
{
    public record CreateRunCommand(Guid UserId, CreateRunDto CreateRunDto) : IRequest<ApiResponse<int>>;
}
