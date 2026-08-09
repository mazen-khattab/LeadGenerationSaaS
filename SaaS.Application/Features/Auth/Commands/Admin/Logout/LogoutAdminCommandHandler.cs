using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;

namespace SaaS.Application.Features.Auth.Commands.Admin.Logout
{
    public class LogoutAdminCommandHandler : IRequestHandler<LogoutAdminCommand, ApiResponse<string>>
    {
        private readonly IAppDbContext _context;
        private readonly ITokenService _tokenService;

        public LogoutAdminCommandHandler(IAppDbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        public async Task<ApiResponse<string>> Handle(LogoutAdminCommand request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                // Soft-revoke refresh token using ExecuteUpdateAsync for performance
                await _context.SystemAdminRefreshTokens
                    .Where(rt => rt.Token == request.RefreshToken && rt.AdminId == request.AdminId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(rt => rt.ExpDate, DateTime.UtcNow)
                        .SetProperty(rt => rt.IsActive, false), cancellationToken);
            }

            // Clear auth cookies (HTTP-only)
            _tokenService.ClearAuthCookies();
            return ApiResponse<string>.Success(null, "Admin logged out successfully.");
        }
    }
}
