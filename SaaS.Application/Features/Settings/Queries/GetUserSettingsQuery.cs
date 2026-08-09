using MediatR;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Models;
using System;

namespace SaaS.Application.Features.Settings.Queries
{
    public record GetUserSettingsQuery(Guid UserId) : IRequest<ApiResponse<UserSettingsDto>>;
}
