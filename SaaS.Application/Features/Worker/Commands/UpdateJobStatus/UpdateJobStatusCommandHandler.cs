using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SaaS.Application.Features.Worker.Commands.UpdateJobStatus
{
    public class UpdateJobStatusCommandHandler : IRequestHandler<UpdateJobStatusCommand, ApiResponse<bool>>
    {
        private readonly IAppDbContext _context;
        private readonly ILogger<UpdateJobStatusCommandHandler> _logger;

        public UpdateJobStatusCommandHandler(
            IAppDbContext context,
            ILogger<UpdateJobStatusCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApiResponse<bool>> Handle(UpdateJobStatusCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating Job {JobId} status to {Status}", request.JobId, request.Status);

            var job = await _context.Jobs
                .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);

            if (job == null)
            {
                _logger.LogWarning("Job {JobId} not found", request.JobId);
                return ApiResponse<bool>.Failure("Job not found.", ErrorType.NotFound);
            }

            if (Enum.TryParse<JobStatus>(request.Status, true, out var jobStatusEnum))
            {
                job.Status = jobStatusEnum.ToDbString();

                if (jobStatusEnum == JobStatus.COMPLETED || jobStatusEnum == JobStatus.FAILED)
                {
                    try 
                    {
                        var payload = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(job.PayloadJson);
                        if (payload != null && payload.TryGetValue("accountId", out var accountIdObj) && int.TryParse(accountIdObj.ToString(), out int accountId))
                        {
                            var account = await _context.ConnectedAccounts.FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
                            if (account != null && account.Status == AccountStatus.BUSY.ToDbString())
                            {
                                account.Status = jobStatusEnum == JobStatus.COMPLETED 
                                    ? AccountStatus.COOLING_DOWN.ToDbString() 
                                    : AccountStatus.ACTIVE.ToDbString();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update ConnectedAccount status for Job {JobId}", job.Id);
                    }
                }
            }
            else
            {
                job.Status = request.Status;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Job {JobId} updated successfully to status {Status}", job.Id, job.Status);

            return ApiResponse<bool>.Success(true, "Job status updated successfully.");
        }
    }
}
