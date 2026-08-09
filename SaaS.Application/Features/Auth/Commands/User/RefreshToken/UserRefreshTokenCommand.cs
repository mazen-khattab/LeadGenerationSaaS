using MediatR;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Models;
using SaaS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Features.Auth.Commands.User.RefreshToken
{
    public record UserRefreshTokenCommand(string token) : IRequest<ApiResponse<AuthLoginResponseDto>>;
}
