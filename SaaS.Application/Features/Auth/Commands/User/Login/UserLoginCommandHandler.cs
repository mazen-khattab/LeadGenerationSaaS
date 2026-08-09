using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Application.Common.Settings;
using SaaS.Domain.Entities;
using SaaS.Domain.Enums;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SaaS.Application.Features.Auth.Commands.User.Login
{
    internal class UserLoginCommandHandler : IRequestHandler<UserLoginCommand, ApiResponse<AuthLoginResponseDto>>
    {
        private readonly IAppDbContext _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuthSessionService _authSessionService;
        private readonly ILogger<UserLoginCommandHandler> _logger;

        public UserLoginCommandHandler(
            IAppDbContext db,            
            IPasswordHasher passwordHasher,
            ILogger<UserLoginCommandHandler> logger,
            IAuthSessionService authSessionService)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _logger = logger;
            _authSessionService = authSessionService;
        }

        public async Task<ApiResponse<AuthLoginResponseDto>> Handle(UserLoginCommand request, CancellationToken cancellationToken)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            var utcNow = DateTime.UtcNow;

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == request.email && !u.IsDeleted, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Authentication failed for unknown email: {Email}", request.email);
                return ApiResponse<AuthLoginResponseDto>.Failure("Invalid credentials.", ErrorType.InvalidCredentials);
            }

            var passwordValid = _passwordHasher.VerifyPassword(request.password, user.PasswordHash);
            if (!passwordValid)
            {
                _logger.LogWarning("Authentication failed for user id {UserId}: invalid password.", user.Id);
                return ApiResponse<AuthLoginResponseDto>.Failure("Invalid credentials.", ErrorType.InvalidCredentials);
            }

            // Transparently re-hash weak/legacy password hashes when detected
            try
            {
                if (_passwordHasher.NeedsRehash(user.PasswordHash))
                {
                    user.PasswordHash = _passwordHasher.HashPassword(request.password);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Rehash check failed for user {UserId}", user.Id);
            }

            // Invalidate any previous active refresh tokens for Single Active Session policy
            var sessionToken = Guid.NewGuid().ToString("N");
            user.CurrentSessionToken = sessionToken;

            var loginResponseDto = await _authSessionService.CreateUserSessionAsync(user.Id, user.Email, user.FullName, sessionToken, cancellationToken);
            return ApiResponse<AuthLoginResponseDto>.Success(loginResponseDto, "Login successful.");
        }
    }
}
