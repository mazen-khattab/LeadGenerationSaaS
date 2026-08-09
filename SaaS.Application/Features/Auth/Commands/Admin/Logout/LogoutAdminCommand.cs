using MediatR;
using SaaS.Application.Common.Models;
using System;

namespace SaaS.Application.Features.Auth.Commands.Admin.Logout
{
    public record LogoutAdminCommand(Guid AdminId, string? RefreshToken) : IRequest<ApiResponse<string>>;
}
