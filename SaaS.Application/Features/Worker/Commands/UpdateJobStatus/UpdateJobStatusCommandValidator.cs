using FluentValidation;
using SaaS.Domain.Enums;
using System;

namespace SaaS.Application.Features.Worker.Commands.UpdateJobStatus
{
    public class UpdateJobStatusCommandValidator : AbstractValidator<UpdateJobStatusCommand>
    {
        public UpdateJobStatusCommandValidator()
        {
            RuleFor(x => x.JobId)
                .GreaterThan(0)
                .WithMessage("JobId must be greater than 0.");

            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("Status is required.")
                .Must(BeAValidStatus)
                .WithMessage($"Status must be a valid JobStatus.");
        }

        private bool BeAValidStatus(string status)
        {
            return Enum.TryParse(typeof(JobStatus), status, true, out _);
        }
    }
}
