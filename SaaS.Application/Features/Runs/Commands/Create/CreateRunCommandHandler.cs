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

namespace SaaS.Application.Features.Runs.Commands.Create
{
    public class CreateRunCommandHandler : IRequestHandler<CreateRunCommand, ApiResponse<int>>
    {
        private readonly IAppDbContext _context;
        private readonly IEncryptionService _encryptionService;
        private readonly INetworkClient _networkClient;
        private readonly IN8nWebhookResolver _webhookResolver;
        private readonly IUserBotService _userBotService;

        public CreateRunCommandHandler(
            IAppDbContext context,
            IEncryptionService encryptionService,
            INetworkClient networkClient,
            IN8nWebhookResolver webhookResolver,
            IUserBotService userBotService)
        {
            _context = context;
            _encryptionService = encryptionService;
            _networkClient = networkClient;
            _webhookResolver = webhookResolver;
            _userBotService = userBotService;
        }

        public async Task<ApiResponse<int>> Handle(CreateRunCommand request, CancellationToken cancellationToken)
        {
            var hasBot = await _userBotService.OwnerShipCheck(request.UserId, request.CreateRunDto.BotId, cancellationToken);

            if (!hasBot)
            {
                return ApiResponse<int>.Failure("User or bot not found", ErrorType.NotFound);
            }

            // Fetch connected account with its cookie and bot
            var account = await _context.ConnectedAccounts
                .AsNoTracking()
                .Include(a => a.Cookie)
                .Include(a => a.Bot)
                .FirstOrDefaultAsync(a => a.Id == request.CreateRunDto.ConnectedAccountId, cancellationToken);

            var group = await _context.TargetGroups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == request.CreateRunDto.TargetGroupId, cancellationToken);

            if (account is null)
            {
                return ApiResponse<int>.Failure("Connected account not found.", ErrorType.NotFound);
            }

            // Decrypt cookies
            var encrypted = account.Cookie?.EncryptedCookies ?? string.Empty;
            var decryptedCookies = _encryptionService.Decrypt(encrypted);

            // Check expiry using stored expire date
            var expireDate = account.Cookie?.CookiesExpireDate ?? DateTime.MinValue;
            if (expireDate <= DateTime.UtcNow)
            {
                return ApiResponse<int>.Failure("Your account cookies has been expired. Pls refresh it.", ErrorType.ValidationError);
            }

            // Create run entity and persist initial state
            var run = new Run
            {
                UserId = request.UserId,
                BotId = account.BotId,
                GroupId = request.CreateRunDto.TargetGroupId,
                AccountId = request.CreateRunDto.ConnectedAccountId,
                InfoJson = string.IsNullOrWhiteSpace(request.CreateRunDto.InfoJson) ? "{}" : request.CreateRunDto.InfoJson,
                StartedAt = DateTime.UtcNow,
                Status = RunStatus.RUNNING.ToString()
            };

            await _context.Runs.AddAsync(run, cancellationToken);

            // Resolve webhook URL
            var botCode = account.Bot?.UiModuleCode ?? string.Empty;
            var webhookUrl = _webhookResolver.GetWebhookUrl(botCode);

            // Build payload
            JsonElement infoElement;
            try
            {
                using var doc = JsonDocument.Parse(request.CreateRunDto.InfoJson);
                infoElement = doc.RootElement.Clone();
            }
            catch
            {
                // This should have been validated earlier, but guard anyway
                return ApiResponse<int>.Failure("Invalid JSON format.", ErrorType.BadRequest);
            }

            var payload = new
            {
                RunId = run.Id,
                Cookies = decryptedCookies,
                Info = infoElement
            };

            // Delegate HTTP posting to INetworkClient (implementation is infrastructure-specific and will handle auth)
            var sent = await _networkClient.PostJsonAsync(webhookUrl, payload, ExternalSystem.N8n, cancellationToken);
            if (!sent.IsSuccess)
            {
                return ApiResponse<int>.Failure("Failed to dispatch run to automation engine.", ErrorType.ServerError);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return ApiResponse<int>.Success(run.Id, "Run started successfully.");
        }
    }
}
