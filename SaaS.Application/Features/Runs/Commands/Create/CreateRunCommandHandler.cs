using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Domain.Entities;
using MediatR;
using SaaS.Domain.Enums;
using Microsoft.Extensions.Logging;
using SaaS.Domain.Extensions;

namespace SaaS.Application.Features.Runs.Commands.Create
{
    public class CreateRunCommandHandler : IRequestHandler<CreateRunCommand, ApiResponse<int>>
    {
        private readonly IAppDbContext _context;
        private readonly IEncryptionService _encryptionService;
        private readonly INetworkClient _networkClient;
        private readonly IN8nWebhookResolver _webhookResolver;
        private readonly IUserBotService _userBotService;
        private readonly ILogger<CreateRunCommandHandler> _logger;

        public CreateRunCommandHandler(
            IAppDbContext context,
            IEncryptionService encryptionService,
            INetworkClient networkClient,
            IN8nWebhookResolver webhookResolver,
            IUserBotService userBotService,
            ILogger<CreateRunCommandHandler> logger)
        {
            _context = context;
            _encryptionService = encryptionService;
            _networkClient = networkClient;
            _webhookResolver = webhookResolver;
            _userBotService = userBotService;
            _logger = logger;
        }

        public async Task<ApiResponse<int>> Handle(CreateRunCommand request, CancellationToken cancellationToken)
        {
            var userId = request.UserId;
            var botId = request.CreateRunDto.BotId;
            var connectedAccountId = request.CreateRunDto.ConnectedAccountId;
            int? targetGroupId = request.CreateRunDto.TargetGroupId;

            _logger.LogInformation("Starting run creation. UserId: {UserId}, BotId: {BotId}, AccountId: {ConnectedAccountId}, GroupId: {TargetGroupId}",
                userId, botId, connectedAccountId, targetGroupId);

            _logger.LogDebug("Checking bot ownership. UserId: {UserId}, BotId: {BotId}", userId, botId);

            var hasBot = await _userBotService.OwnerShipCheck(userId, botId, cancellationToken);

            if (!hasBot)
            {
                _logger.LogWarning("Bot ownership check failed. UserId: {UserId}, BotId: {BotId}", userId, botId);
                return ApiResponse<int>.Failure("User or bot not found", ErrorType.NotFound);
            }
            _logger.LogDebug("Bot ownership confirmed. UserId: {UserId}, BotId: {BotId}", userId, botId);

            _logger.LogDebug("Loading connected account. ConnectedAccountId: {ConnectedAccountId}", connectedAccountId);
            // Fetch connected account with its cookie and bot
            var account = await _context.ConnectedAccounts
                .Include(a => a.Cookie)
                .Include(a => a.Bot)
                .FirstOrDefaultAsync(a => a.Id == connectedAccountId && a.UserId == userId, cancellationToken);

            if (account is null)
            {
                _logger.LogWarning("Connected account was not found. ConnectedAccountId: {ConnectedAccountId}", connectedAccountId);
                return ApiResponse<int>.Failure("Connected account not found.", ErrorType.NotFound);
            }

            if (!string.Equals(account.Status, AccountStatus.ACTIVE.ToDbString(), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Account {AccountId} is not active. Current status: {Status}", connectedAccountId, account.Status);
                return ApiResponse<int>.Failure($"Account is currently {account.Status} and cannot be used.", ErrorType.ValidationError);
            }

            _logger.LogDebug("Loading target group. TargetGroupId: {TargetGroupId}", targetGroupId);
            var group = await _context.TargetGroups
                .AsNoTracking()
                .AnyAsync(g => g.Id == targetGroupId && g.UserId == userId, cancellationToken);

            // Return NotFound if the request already GroupId that not exit in the DB
            if (!group && targetGroupId > 0)
            {
                _logger.LogWarning("Target group was not found. TargetGroupId: {TargetGroupId}", targetGroupId);
                return ApiResponse<int>.Failure("Target group not found.", ErrorType.NotFound);
            }
            _logger.LogDebug("Connected account and target group loaded successfully. ConnectedAccountId: {ConnectedAccountId}, TargetGroupId: {TargetGroupId}", 
                connectedAccountId, targetGroupId);

            _logger.LogDebug("Fetch user company info for UserId: {UserId}", userId);
            var companyInfo = await _context.UserSettings
                .AsNoTracking()
                .Select(us => new {
                    us.UserId,
                    us.CompanyName,
                    us.CompanyPitch
                })
                .FirstOrDefaultAsync(us => us.UserId == userId, cancellationToken);

            if (companyInfo is null || string.IsNullOrEmpty(companyInfo.CompanyName) || string.IsNullOrEmpty(companyInfo.CompanyPitch))
            {
                _logger.LogWarning("The compnay info is missing or not complete for UserId: {UserId} --- CompandyName: {CompanyName}, CompanyPitch: {CompanyPitch}",
                    userId, companyInfo?.CompanyName ?? "No company name", companyInfo?.CompanyPitch ?? "No company pitch");
                return ApiResponse<int>.Failure("Some user settings is missing.", ErrorType.NotFound);
            }

            _logger.LogDebug("Decrypting cookies for connected account. ConnectedAccountId: {ConnectedAccountId}", connectedAccountId);
            // Decrypt cookies
            var encrypted = account.Cookie?.EncryptedCookies ?? string.Empty;
            var decryptedCookies = _encryptionService.Decrypt(encrypted);

            // Check expiry using stored expire date
            var expireDate = account.Cookie?.CookiesExpireDate ?? DateTime.MinValue;
            if (expireDate <= DateTime.UtcNow)
            {
                _logger.LogWarning("Cookies have expired. ConnectedAccountId: {ConnectedAccountId}, ExpireDate: {ExpireDate}", connectedAccountId, expireDate);
                return ApiResponse<int>.Failure("Your account cookies has been expired. Pls refresh it.", ErrorType.ValidationError);
            }

            // Create run entity and persist initial state
            var run = new Run
            {
                UserId = userId,
                BotId = account.BotId,
                AccountId = connectedAccountId,
                InfoJson = request.CreateRunDto.InfoJson,
                StartedAt = DateTime.UtcNow,
                Status = RunStatus.RUNNING.ToDbString()
            };

            if (targetGroupId > 0)
                run.GroupId = targetGroupId;

            // Set Account Status to BUSY
            account.Status = AccountStatus.BUSY.ToDbString();

            // 1. Save Run to DB (Without holding transaction open during HTTP call)
            await _context.Runs.AddAsync(run, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Run persisted successfully. RunId: {RunId}, UserId: {UserId}", run.Id, userId);

            try
            {
                // Resolve webhook URL
                var botCode = account.Bot?.UiModuleCode ?? string.Empty;
                var webhookUrl = _webhookResolver.GetWebhookUrl(botCode);

                // Build payload
                using var doc = JsonDocument.Parse(request.CreateRunDto.InfoJson);
                JsonElement infoElement = doc.RootElement.Clone();

                var payload = new
                {
                    RunId = run.Id,
                    Cookies = decryptedCookies,
                    Info = infoElement,
                    companyInfo.CompanyName,
                    companyInfo.CompanyPitch,
                };

                _logger.LogInformation("Sending run payload to external system. RunId: {RunId}, ExternalSystem: {ExternalSystem}", 
                    run.Id, ExternalSystem.N8n);

                // 2. Make External HTTP Call
                var sent = await _networkClient.PostJsonAsync(webhookUrl, payload, ExternalSystem.N8n, cancellationToken);

                if (!sent.IsSuccess)
                {
                    throw new InvalidOperationException(sent.ErrorMessage ?? "External request failed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch run to external system. RunId: {RunId}", run.Id);
                
                // 3. Compensation: Mark as failed if dispatch fails
                run.Status = RunStatus.FAILED.ToDbString();
                account.Status = AccountStatus.ACTIVE.ToDbString();
                await _context.SaveChangesAsync(cancellationToken);
                
                return ApiResponse<int>.Failure("Failed to dispatch run to external system.", ErrorType.ServerError);
            }

            _logger.LogInformation("Run started successfully. RunId: {RunId}, UserId: {UserId}", run.Id, userId);
            return ApiResponse<int>.Success(run.Id, "Run started successfully.");
        }
    }
}
