using SaaS.Application.Common.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Common.Interfaces
{
    public interface IAuthSessionService
    {
        Task<AuthLoginResponseDto> CreateUserSessionAsync(
            Guid userId,
            string email,
            string fullName,
            string sessionToken,
            CancellationToken cancellationToken);

        Task<AuthLoginResponseDto> CreateAdminSessionAsync(
            Guid adminId,
            string email,
            string fullName,
            string role,
            CancellationToken cancellationToken);
    }
}
