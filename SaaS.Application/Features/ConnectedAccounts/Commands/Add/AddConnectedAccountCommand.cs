using MediatR;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Models;
using System;

namespace SaaS.Application.Features.ConnectedAccounts.Commands.Add
{
    public record AddConnectedAccountCommand(Guid UserId, AddConnectedAccountDto AccountDto) : IRequest<ApiResponse<Guid>>;
}
