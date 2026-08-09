using MediatR;
using SaaS.Application.Common.Models;
using System;

namespace SaaS.Application.Features.Auth.Commands.User.Logout
{
    public record LogoutUserCommand(Guid UserId, string? RefreshToken) : IRequest<ApiResponse<string>>;
}
