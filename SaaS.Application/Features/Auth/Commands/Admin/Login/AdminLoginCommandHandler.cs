using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Domain.Entities;

namespace SaaS.Application.Features.Auth.Commands.Admin.Login
{
    internal class AdminLoginCommandHandler : IRequestHandler<AdminLoginCommand, ApiResponse<AuthLoginResponseDto>>
    {
        private readonly IAppDbContext _db;
        private readonly IPasswordHasherService _passwordHasher;
        private readonly IAuthSessionService _authSessionService;
        private readonly ILogger<AdminLoginCommandHandler> _logger;

        public AdminLoginCommandHandler(
            IAppDbContext db,
            IPasswordHasherService passwordHasher,
            ILogger<AdminLoginCommandHandler> logger,
            IAuthSessionService authSessionService)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _logger = logger;
            _authSessionService = authSessionService;
        }

        public async Task<ApiResponse<AuthLoginResponseDto>> Handle(AdminLoginCommand request, CancellationToken cancellationToken)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            var utcNow = DateTime.UtcNow;

            var admin = await _db.SystmeAdmins
                .FirstOrDefaultAsync(a => a.Email == request.email && a.IsActive, cancellationToken);

            if (admin == null)
            {
                _logger.LogWarning("Authentication failed for unknown email: {Email}", request.email);
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            var passwordValid = _passwordHasher.VerifyPassword(request.password, admin.PasswordHash);
            if (!passwordValid)
            {
                _logger.LogWarning("Authentication failed for admin id {AdminId}: invalid password.", admin.Id);
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            // Transparently re-hash weak/legacy password hashes when detected
            try
            {
                if (_passwordHasher.NeedsRehash(admin.PasswordHash))
                {
                    admin.PasswordHash = _passwordHasher.HashPassword(request.password);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Rehash check failed for admin {AdminId}", admin.Id);
            }

            var authLoginResponseDto = await _authSessionService.CreateAdminSessionAsync(admin.Id, admin.Email, admin.FullName, admin.Role, cancellationToken);
            return ApiResponse<AuthLoginResponseDto>.Success(authLoginResponseDto, "Admin Login successfully");
        }
    }
}
