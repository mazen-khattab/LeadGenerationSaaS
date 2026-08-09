using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Features.Auth.Commands.User.RefreshToken
{
    public class UserRefreshTokenCommandHandler : IRequestHandler<UserRefreshTokenCommand, ApiResponse<AuthLoginResponseDto>>
    {
        private readonly IAppDbContext _db;
        private readonly IAuthSessionService _authSessionService;
        private readonly ILogger<UserRefreshTokenCommandHandler> _logger;

        public UserRefreshTokenCommandHandler(
            IAppDbContext db,
            IAuthSessionService authSessionService,
            ILogger<UserRefreshTokenCommandHandler> logger)
        {
            _db = db;
            _authSessionService = authSessionService;
            _logger = logger;
        }

        public async Task<ApiResponse<AuthLoginResponseDto>> Handle(UserRefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var tokenValue = request.token;
            if (string.IsNullOrWhiteSpace(tokenValue))
            {
                _logger.LogWarning("Refresh token attempt failed: Token string is missing or whitespace.");
                return ApiResponse<AuthLoginResponseDto>.Failure("Refresh token is missing or empty.", ErrorType.Unauthorized);
            }

            var dbToken = await _db.UserRefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == tokenValue, cancellationToken);

            if (dbToken is null)
            {
                _logger.LogWarning("Refresh token attempt rejected: token not found.");
                return ApiResponse<AuthLoginResponseDto>.Failure("Refresh token invalid or expired.", ErrorType.Unauthorized);
            }

            // Checked IsActive as well to match the new strict DB state
            if (dbToken.ExpDate <= DateTime.UtcNow || !dbToken.IsActive || dbToken.User.IsDeleted)
            {
                _logger.LogWarning("Refresh token attempt failed for user {UserId}: IsActive={IsActive}, ExpDate={ExpDate} IsDeleted={IsDeleted}.",
                    dbToken.UserId, dbToken.IsActive, dbToken.ExpDate, dbToken.User.IsDeleted);

                return ApiResponse<AuthLoginResponseDto>.Failure("Refresh token invalid or expired.", ErrorType.Unauthorized);
            }

            var user = dbToken.User;

            var sessionToken = Guid.NewGuid().ToString("N");
            user.CurrentSessionToken = sessionToken;

            var authLoginResponseDto = await _authSessionService.CreateUserSessionAsync(user.Id, user.Email, user.FullName, sessionToken, cancellationToken);
            return ApiResponse<AuthLoginResponseDto>.Success(authLoginResponseDto, "Refresh token successful.");
        }
    }
}
