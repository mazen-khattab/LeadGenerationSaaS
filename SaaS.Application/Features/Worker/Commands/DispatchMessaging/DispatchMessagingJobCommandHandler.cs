using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Application.Common.Dtos;
using SaaS.Domain.Entities;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;
using MediatR;

namespace SaaS.Application.Features.Worker.Commands.DispatchMessaging
{
    public class DispatchMessagingJobCommandHandler : IRequestHandler<DispatchMessagingJobCommand, ApiResponse<DispatchMessagingResultDto>>
    {
        private readonly IAppDbContext _dbContext;
        private readonly IEncryptionService _encryptionService;
        private readonly IUserBotService _userBotService;
        private readonly INetworkClient _networkClient;
        private readonly ILogger<DispatchMessagingJobCommandHandler> _logger;

        public DispatchMessagingJobCommandHandler(
            IAppDbContext dbContext,
            IEncryptionService encryptionService,
            IUserBotService userBotService,
            INetworkClient networkClient,
            ILogger<DispatchMessagingJobCommandHandler> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
            _userBotService = userBotService ?? throw new ArgumentNullException(nameof(userBotService));
            _networkClient = networkClient ?? throw new ArgumentNullException(nameof(networkClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApiResponse<DispatchMessagingResultDto>> Handle(DispatchMessagingJobCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting DispatchMessagingJob for UserId: {UserId}, BotId: {BotId} with {LeadCount} leads.", 
                request.UserId, request.BotId, request.LeadIds?.Count ?? 0);

            // Ownership check
            _logger.LogDebug("Checking ownership of BotId: {BotId} for UserId: {UserId}", request.BotId, request.UserId);
            var hasBot = await _userBotService.OwnerShipCheck(request.UserId, request.BotId, cancellationToken);

            if (!hasBot)
            {
                _logger.LogWarning("Ownership check failed for UserId: {UserId}, BotId: {BotId}.", request.UserId, request.BotId);
                return ApiResponse<DispatchMessagingResultDto>.Failure("User or bot not found", ErrorType.NotFound);
            }

            // Fetch active connected account for this bot and user including cookie
            _logger.LogDebug("Fetching active connected account for UserId: {UserId}, AccountId: {AccountId}", request.UserId, request.AccountId);
            var account = await _dbContext.ConnectedAccounts
                .AsNoTracking()
                .Select(a => new
                {
                    a.Id,
                    a.UserId,
                    a.IsActive,
                    a.Cookie.EncryptedCookies,
                    a.Cookie.CookiesExpireDate,
                    a.Bot,
                })
                .FirstOrDefaultAsync(a => a.UserId == request.UserId && a.Id == request.AccountId && a.IsActive, cancellationToken);

            if (account is null)
            {
                _logger.LogWarning("No active connected account found for UserId: {UserId}, AccountId: {AccountId}.", request.UserId, request.AccountId);
                return ApiResponse<DispatchMessagingResultDto>.Failure("No active connected account found for this bot.", ErrorType.NotFound);
            }

            var encrypted = account.EncryptedCookies ?? string.Empty;
            var decryptedCookies = _encryptionService.Decrypt(encrypted);

            var expireDate = account.CookiesExpireDate;
            if (expireDate <= DateTime.UtcNow || string.IsNullOrWhiteSpace(decryptedCookies))
            {
                _logger.LogWarning("Account cookies are expired or invalid for UserId: {UserId}, AccountId: {AccountId}. ExpireDate: {ExpireDate}", 
                    request.UserId, request.AccountId, expireDate);
                return ApiResponse<DispatchMessagingResultDto>.Failure("Your account cookies has been expired. Pls refresh it.", ErrorType.ValidationError);
            }

            // Validate leads
            _logger.LogDebug("Validating {LeadCount} leads for UserId: {UserId}, BotId: {BotId}", request.LeadIds!.Count, request.UserId, request.BotId);
            var leads = await _dbContext.Leads
                .AsNoTracking()
                .Where(l => request.LeadIds.Contains(l.Id) && l.UserId == request.UserId && l.BotId == request.BotId)
                .ToListAsync(cancellationToken);

            if (leads.Count != request.LeadIds.Count)
            {
                _logger.LogWarning("Lead validation failed. Expected {ExpectedCount}, but found {FoundCount} valid leads for UserId: {UserId}, BotId: {BotId}.", 
                    request.LeadIds.Count, leads.Count, request.UserId, request.BotId);
                return ApiResponse<DispatchMessagingResultDto>.Failure("One or more leads are invalid or do not belong to the user/bot.", ErrorType.ValidationError);
            }

            if (leads.Any(l => !string.Equals(l.Status, LeadStatus.PENDING.ToDbString(), StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Lead validation failed. One or more leads are not in PENDING state for UserId: {UserId}, BotId: {BotId}.", request.UserId, request.BotId);
                return ApiResponse<DispatchMessagingResultDto>.Failure("One or more leads are not in Pending state.", ErrorType.ValidationError);
            }

            // Create Job record
            _logger.LogDebug("Creating MESSAGING job record for UserId: {UserId}, BotId: {BotId}", request.UserId, request.BotId);
            var payloadObj = new { leadIds = request.LeadIds };

            var job = new Job
            {
                UserId = request.UserId,
                BotId = request.BotId,
                Type = JobType.MESSAGING.ToDbString(),
                Status = JobStatus.PROCESSING.ToDbString(),
                CreatedAt = DateTime.UtcNow,
                PayloadJson = JsonSerializer.Serialize(payloadObj)
            };

            await _dbContext.Jobs.AddAsync(job, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully created MESSAGING job with JobId: {JobId} for UserId: {UserId}.", job.Id, request.UserId);

            // Build worker payload
            var workerPayload = new
            {
                jobId = job.Id,
                userId = request.UserId.ToString(),
                botId = request.BotId,
                cookies = decryptedCookies,
                leads = leads.Select(l => new { id = l.Id, profileName = l.ProfileName, profileUrl = l.ProfileUrl, aiMessage = l.AiMessage ?? string.Empty }).ToArray()
            };

            string endpoint = "/api/worker/dispatch-messaging";

            try
            {
                // Send to Node worker
                _logger.LogDebug("Dispatching job {JobId} to Node worker at endpoint {Endpoint}", job.Id, endpoint);
                var result = await _networkClient.PostJsonAsync(endpoint, workerPayload, ExternalSystem.NodeWorker, cancellationToken);

                if (!result.IsSuccess)
                {
                    _logger.LogError("Failed to reach Node worker for JobId: {JobId}. Marking job as FAILED.", job.Id);
                    // Update job status to Failed
                    job.Status = JobStatus.FAILED.ToDbString();
                    await _dbContext.SaveChangesAsync(CancellationToken.None);

                    return ApiResponse<DispatchMessagingResultDto>.Failure("Failed to reach worker server. Please try again later.", ErrorType.ServerError);
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Dispatch operation timed out or was cancelled for JobId: {JobId}. Marking job as FAILED.", job.Id);

                job.Status = JobStatus.FAILED.ToDbString();
                await _dbContext.SaveChangesAsync(CancellationToken.None); // Ensure save goes through
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while dispatching JobId: {JobId} to Node worker. Marking job as FAILED.", job.Id);

                job.Status = JobStatus.FAILED.ToDbString();
                await _dbContext.SaveChangesAsync(CancellationToken.None);

                return ApiResponse<DispatchMessagingResultDto>.Failure("An unexpected error occurred while dispatching the job.", ErrorType.ServerError);
            }

            _logger.LogInformation("Job {JobId} successfully accepted by Node worker for background processing.", job.Id);
            var responseDto = new DispatchMessagingResultDto(job.Id, job.Status, request.LeadIds.Count);
            return ApiResponse<DispatchMessagingResultDto>.Success(responseDto, "Job accepted successfully.");
        }
    }
}
