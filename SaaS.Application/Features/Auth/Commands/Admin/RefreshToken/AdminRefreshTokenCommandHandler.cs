using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SaaS.Application.Features.Auth.Commands.Admin.RefreshToken
{
    internal class AdminRefreshTokenCommandHandler : IRequestHandler<AdminRefreshTokenCommand, ApiResponse<AuthLoginResponseDto>>
    {
        private readonly IAppDbContext _db;
        private readonly IAuthSessionService _authSessionService;
        private readonly ILogger<AdminRefreshTokenCommandHandler> _logger;

        public AdminRefreshTokenCommandHandler(
            IAppDbContext db,
            IAuthSessionService authSessionService,
            ILogger<AdminRefreshTokenCommandHandler> logger)
        {
            _db = db;
            _authSessionService = authSessionService;
            _logger = logger;
        }

        public async Task<ApiResponse<AuthLoginResponseDto>> Handle(AdminRefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var tokenValue = request.token;
            if (string.IsNullOrWhiteSpace(tokenValue))
            {
                _logger.LogWarning("Refresh token attempt failed: Token string is missing or whitespace.");
                return ApiResponse<AuthLoginResponseDto>.Failure("Refresh token is missing or empty.", ErrorType.Unauthorized);
            }

            var dbToken = await _db.SystemAdminRefreshTokens
                .Include(rt => rt.Admin)
                .FirstOrDefaultAsync(rt => rt.Token == tokenValue, cancellationToken);

            if (dbToken is null)
            {
                _logger.LogWarning("Refresh token attempt rejected: token not found.");
                return ApiResponse<AuthLoginResponseDto>.Failure("Refresh token invalid or expired.", ErrorType.Unauthorized);
            }

            // Check IsActive and ExpDate to match the strict DB state
            if (dbToken.ExpDate <= DateTime.UtcNow || !dbToken.IsActive || dbToken.Admin.IsActive == false)
            {
                _logger.LogWarning("Refresh token attempt failed for admin {AdminId}: IsActive={IsActive}, ExpDate={ExpDate} AdminIsActive={AdminIsActive}.",
                    dbToken.AdminId, dbToken.IsActive, dbToken.ExpDate, dbToken.Admin.IsActive);
                return ApiResponse<AuthLoginResponseDto>.Failure("Refresh token invalid or expired.", ErrorType.Unauthorized);
            }

            var admin = dbToken.Admin;

            var authLoginResponseDto = await _authSessionService.CreateAdminSessionAsync(admin.Id, admin.Email, admin.FullName, admin.Role, cancellationToken);
            return ApiResponse<AuthLoginResponseDto>.Success(authLoginResponseDto, "Refresh token successful.");
        }
    }
}
