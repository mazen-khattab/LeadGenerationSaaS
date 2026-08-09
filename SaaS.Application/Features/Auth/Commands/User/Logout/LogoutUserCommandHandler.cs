using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;

namespace SaaS.Application.Features.Auth.Commands.User.Logout
{
    public class LogoutUserCommandHandler : IRequestHandler<LogoutUserCommand, ApiResponse<string>>
    {
        private readonly IAppDbContext _context;
        private readonly ITokenService _tokenService;

        public LogoutUserCommandHandler(IAppDbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        public async Task<ApiResponse<string>> Handle(LogoutUserCommand request, CancellationToken cancellationToken)
        {
            await _context.Users
                .Where(u => u.Id == request.UserId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.CurrentSessionToken, (string?)null), cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                // Soft-revoke refresh token using ExecuteUpdateAsync for performance
                await _context.UserRefreshTokens
                    .Where(rt => rt.Token == request.RefreshToken && rt.UserId == request.UserId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(rt => rt.ExpDate, DateTime.UtcNow)
                        .SetProperty(rt => rt.IsActive, false), cancellationToken);
            }

            // Clear auth cookies (HTTP-only)
            _tokenService.ClearAuthCookies();

            return ApiResponse<string>.Success(null, "User logged out successfully.");
        }
    }
}
