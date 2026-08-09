using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Domain.Entities;

namespace SaaS.Application.Features.Settings.Commands.Manage
{
    public class ManageUserSettingsCommandHandler : IRequestHandler<ManageUserSettingsCommand, ApiResponse<Guid>>
    {
        private readonly IAppDbContext _context;
        private readonly IEncryptionService _encryptionService;

        public ManageUserSettingsCommandHandler(IAppDbContext context, IEncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        public async Task<ApiResponse<Guid>> Handle(ManageUserSettingsCommand request, CancellationToken cancellationToken)
        {
            string? encryptedAiKey = null;
            string? encryptedScraperToken = null;

            if (!string.IsNullOrWhiteSpace(request.AIApiKey))
            {
                encryptedAiKey = _encryptionService.Encrypt(request.AIApiKey);
            }

            if (!string.IsNullOrWhiteSpace(request.ScraperToken))
            {
                encryptedScraperToken = _encryptionService.Encrypt(request.ScraperToken);
            }

            var setting = await _context.UserSettings
                .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);

            if (setting != null)
            {
                if (encryptedAiKey != null)
                    setting.AIApiKeyEncrypted = encryptedAiKey; 

                if (encryptedScraperToken != null)
                    setting.ScraperApiTokenEncrypted = encryptedScraperToken;

                setting.DailyMessageLimit = request.DailyLeadLimit;
            }
            else
            {
                var settings = new UserSetting
                {
                    UserId = request.UserId,
                    ScraperApiTokenEncrypted = encryptedAiKey,
                    AIApiKeyEncrypted = encryptedScraperToken,
                    DailyMessageLimit = request.DailyLeadLimit
                };

                await _context.UserSettings.AddAsync(settings, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<Guid>.Success(request.UserId, "Target group managed successfully");
        }
    }
}
