using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Application.Common.Dtos;
using SaaS.Domain.Entities;
using SaaS.Domain.Enums;
using MediatR;

namespace SaaS.Application.Features.Worker.Commands.DispatchMessaging
{
    public class DispatchMessagingJobCommandHandler : IRequestHandler<DispatchMessagingJobCommand, ApiResponse<DispatchMessagingResultDto>>
    {
        private readonly IAppDbContext _dbContext;
        private readonly IEncryptionService _encryptionService;
        private readonly IUserBotService _userBotService;
        private readonly INetworkClient _networkClient;

        public DispatchMessagingJobCommandHandler(
            IAppDbContext dbContext,
            IEncryptionService encryptionService,
            IUserBotService userBotService,
            INetworkClient networkClient)
        {
            _dbContext = dbContext;
            _encryptionService = encryptionService;
            _userBotService = userBotService;
            _networkClient = networkClient;
        }

        public async Task<ApiResponse<DispatchMessagingResultDto>> Handle(DispatchMessagingJobCommand request, CancellationToken cancellationToken)
        {
            // Ownership check
            var hasBot = await _userBotService.OwnerShipCheck(request.UserId, request.BotId, cancellationToken);
            if (!hasBot)
            {
                return ApiResponse<DispatchMessagingResultDto>.Failure("User or bot not found", ErrorType.NotFound);
            }

            // Fetch active connected account for this bot and user including cookie
            var account = await _dbContext.ConnectedAccounts
                .AsNoTracking()
                .Include(a => a.Cookie)
                .Include(a => a.Bot)
                .FirstOrDefaultAsync(a => a.UserId == request.UserId && a.BotId == request.BotId && a.IsActive, cancellationToken);

            if (account is null)
            {
                return ApiResponse<DispatchMessagingResultDto>.Failure("No active connected account found for this bot.", ErrorType.NotFound);
            }

            var encrypted = account.Cookie?.EncryptedCookies ?? string.Empty;
            var decryptedCookies = _encryptionService.Decrypt(encrypted);

            var expireDate = account.Cookie?.CookiesExpireDate ?? DateTime.MinValue;
            if (expireDate <= DateTime.UtcNow || string.IsNullOrWhiteSpace(decryptedCookies))
            {
                return ApiResponse<DispatchMessagingResultDto>.Failure("Your account cookies has been expired. Pls refresh it.", ErrorType.ValidationError);
            }

            // Validate leads
            var leads = await _dbContext.Leads
                .Where(l => request.LeadIds.Contains(l.Id) && l.UserId == request.UserId && l.BotId == request.BotId)
                .ToListAsync(cancellationToken);

            if (leads.Count != request.LeadIds.Count)
            {
                return ApiResponse<DispatchMessagingResultDto>.Failure("One or more leads are invalid or do not belong to the user/bot.", ErrorType.ValidationError);
            }

            if (leads.Any(l => !string.Equals(l.Status, LeadStatus.PENDING.ToString(), StringComparison.OrdinalIgnoreCase)))
            {
                return ApiResponse<DispatchMessagingResultDto>.Failure("One or more leads are not in Pending state.", ErrorType.ValidationError);
            }

            // Create Job record
            var payloadObj = new { leadIds = request.LeadIds };

            var job = new Job
            {
                UserId = request.UserId,
                BotId = request.BotId,
                Type = "MESSAGING",
                Status = "Processing",
                CreatedAt = DateTime.UtcNow,
                PayloadJson = JsonSerializer.Serialize(payloadObj)
            };

            await _dbContext.Jobs.AddAsync(job, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Build worker payload
            var workerPayload = new
            {
                jobId = job.Id,
                userId = request.UserId,
                botId = request.BotId,
                cookies = decryptedCookies,
                leads = leads.Select(l => new { id = l.Id, profileName = l.ProfileName, profileUrl = l.ProfileUrl, aiMessage = l.AiMessage ?? string.Empty }).ToArray()
            };

            string endpoint = "/api/worker/dispatch-messaging";

            // Send to Node worker
            var result = await _networkClient.PostJsonAsync(endpoint, workerPayload, ExternalSystem.NodeWorker, cancellationToken);

            if (!result.IsSuccess)
            {
                // Update job status to Failed
                job.Status = "Failed";
                await _dbContext.SaveChangesAsync(cancellationToken);

                return ApiResponse<DispatchMessagingResultDto>.Failure("Failed to reach worker server. Please try again later.", ErrorType.ServerError);
            }

            var responseDto = new DispatchMessagingResultDto(job.Id, job.Status, request.LeadIds.Count);
            return ApiResponse<DispatchMessagingResultDto>.Success(responseDto, "Job dispatched successfully.");
        }
    }
}
