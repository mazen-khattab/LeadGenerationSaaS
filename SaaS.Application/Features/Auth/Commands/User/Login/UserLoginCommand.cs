using MediatR;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Features.Auth.Commands.User.Login
{
    public record UserLoginCommand(string email, string password) : IRequest<ApiResponse<AuthLoginResponseDto>>;
}
