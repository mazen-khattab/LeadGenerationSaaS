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
