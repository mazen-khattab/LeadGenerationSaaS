using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Settings;
using SaaS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Common.Services
{
    public class AuthSessionService : IAuthSessionService
    {
        private readonly IAppDbContext _db;
        private readonly ITokenService _tokenService;
        private readonly SecuritySettings _securitySettings;
        private readonly ILogger<AuthSessionService> _logger;

        public AuthSessionService(
            IAppDbContext db,
            ITokenService tokenService,
            IOptionsMonitor<SecuritySettings> securityOptions,
            ILogger<AuthSessionService> logger)
        {
            _db = db;
            _tokenService = tokenService;
            _securitySettings = securityOptions?.CurrentValue ?? throw new ArgumentNullException(nameof(securityOptions));
            _logger = logger;
        }

        public async Task<AuthLoginResponseDto> CreateUserSessionAsync(Guid userId, string email, string fullName, string sessionToken, CancellationToken cancellationToken)
        {
            var utcNow = DateTime.UtcNow;

            var accessToken = _tokenService.GenerateAccessToken(userId, email, sessionToken);
            var refreshTokenValue = _tokenService.GenerateRefreshToken();

            var refreshToken = new UserRefreshToken
            {
                UserId = userId,
                Token = refreshTokenValue,
                CreatedAt = utcNow,
                ExpDate = utcNow.AddDays(_securitySettings.RefreshTokenExpirationDays),
                IsActive = true
            };

            // Required because EnableRetryOnFailure() is enabled on the DbContext.
            // Any manually-started transaction must be wrapped in an execution strategy,
            // otherwise EF Core throws InvalidOperationException at runtime.
            var strategy = _db.Database.CreateExecutionStrategy();

            // True only if THIS specific call is the one that actually persisted its
            // tokens to the database. Used below to decide whether to set cookies.
            var tokensPersisted = false;

            await strategy.ExecuteAsync(async () =>
            {
                // Transaction must live inside the lambda: if the strategy retries due to
                // a transient failure, it re-runs the whole delegate, including starting
                // a fresh transaction — reusing an outer transaction here would be wrong.
                await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    // Revoke any previous active token for this user. ExecuteUpdateAsync
                    // issues an immediate UPDATE (bypasses the change tracker), so both
                    // IsActive and ExpDate must be set together here — a token marked
                    // inactive should also reflect its real expiry moment for consistency.
                    await _db.UserRefreshTokens
                        .Where(rt => rt.UserId == userId && rt.IsActive)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(r => r.IsActive, false)
                            .SetProperty(r => r.ExpDate, utcNow), cancellationToken);

                    _db.UserRefreshTokens.Add(refreshToken);

                    // If a concurrent request (e.g. a double-click) is racing this one,
                    // the unique filtered index (UserId WHERE IsActive = 1) will reject
                    // whichever request's SaveChangesAsync arrives second.
                    await _db.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    tokensPersisted = true;
                }
                catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                {
                    await transaction.RollbackAsync(cancellationToken);

                    // Not a real failure from the user's point of view: another
                    // concurrent request for the same user already won and persisted
                    // the active session. We simply don't create a second one, and we
                    // will not throw — the caller still gets a successful response.
                    _logger.LogInformation(
                        "Concurrent session request detected for User {UserId}. " +
                        "Another request already established the active session; " +
                        "this request will not persist a duplicate.",
                        userId);

                    tokensPersisted = false;
                }
                catch
                {
                    // Any other failure (timeout, connection drop, etc.) must roll back
                    // and propagate — this is a genuine error, unlike the case above.
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            if (tokensPersisted)
            {
                // Only the request that actually won the race sets cookies. Setting
                // cookies for the losing request would point the browser at a refresh
                // token that was never saved to the database.
                _tokenService.SetAuthCookies(accessToken, refreshTokenValue);
                _logger.LogInformation("Tokens successfully issued and persisted for User {UserId}", userId);
            }

            // Same response shape either way — the user's identity data doesn't depend
            // on which concurrent request happened to win the race.
            return new AuthLoginResponseDto(userId.ToString(), email, fullName, "User");
        }

        public async Task<AuthLoginResponseDto> CreateAdminSessionAsync(Guid adminId, string email, string fullName, string role, CancellationToken cancellationToken)
        {
            var utcNow = DateTime.UtcNow;

            var accessToken = _tokenService.GenerateAdminAccessToken(adminId, email, role);
            var refreshTokenValue = _tokenService.GenerateRefreshToken();

            var refreshToken = new SystemAdminRefreshTokens
            {
                AdminId = adminId,
                Token = refreshTokenValue,
                CreatedAt = utcNow,
                ExpDate = utcNow.AddDays(_securitySettings.RefreshTokenExpirationDays),
                IsActive = true
            };

            // Required because EnableRetryOnFailure() is enabled on the DbContext.
            // Any manually-started transaction must be wrapped in an execution strategy,
            // otherwise EF Core throws InvalidOperationException at runtime.
            var strategy = _db.Database.CreateExecutionStrategy();

            // True only if THIS specific call is the one that actually persisted its
            // tokens to the database. Used below to decide whether to set cookies.
            var tokensPersisted = false;
            
            await strategy.ExecuteAsync(async () =>
            {
                // Transaction must live inside the lambda: if the strategy retries due to
                // a transient failure, it re-runs the whole delegate, including starting
                // a fresh transaction — reusing an outer transaction here would be wrong.
                await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    // Revoke any previous active token for this admin. ExecuteUpdateAsync
                    // issues an immediate UPDATE (bypasses the change tracker), so both
                    // IsActive and ExpDate must be set together here — a token marked
                    // inactive should also reflect its real expiry moment for consistency.
                    await _db.SystemAdminRefreshTokens
                        .Where(rt => rt.AdminId == adminId && rt.IsActive)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(r => r.IsActive, false)
                            .SetProperty(r => r.ExpDate, utcNow), cancellationToken);

                    _db.SystemAdminRefreshTokens.Add(refreshToken);

                    // If a concurrent request (e.g. a double-click) is racing this one,
                    // the unique filtered index (AdminId WHERE IsActive = 1) will reject
                    // whichever request's SaveChangesAsync arrives second.
                    await _db.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    tokensPersisted = true;
                }
                catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                {
                    await transaction.RollbackAsync(cancellationToken);

                    // Not a real failure from the admin's point of view: another
                    // concurrent request for the same amdmin already won and persisted
                    // the active session. We simply don't create a second one, and we
                    // will not throw — the caller still gets a successful response.
                    _logger.LogInformation(
                        "Concurrent session request detected for Admin {Admin}. " +
                        "Another request already established the active session; " +
                        "this request will not persist a duplicate.",
                        adminId);

                    tokensPersisted = false;
                }
                catch
                {
                    // Any other failure (timeout, connection drop, etc.) must roll back
                    // and propagate — this is a genuine error, unlike the case above.
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            if (tokensPersisted)
            {
                // Only the request that actually won the race sets cookies. Setting
                // cookies for the losing request would point the browser at a refresh
                // token that was never saved to the database.
                _tokenService.SetAuthCookies(accessToken, refreshTokenValue);
                _logger.LogInformation("Tokens successfully issued and persisted for Admin {AdminId}", adminId);
            }

            // Same response shape either way — the admin's identity data doesn't depend
            // on which concurrent request happened to win the race.
            return new AuthLoginResponseDto(adminId.ToString(), email, fullName, role);
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.InnerException is SqlException sqlEx
                && (sqlEx.Number == 2601 || sqlEx.Number == 2627);
        }
    }
}
