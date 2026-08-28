using FluentValidation;
using SaaS.Domain.Enums;
using System;

namespace SaaS.Application.Features.Worker.Commands.UpdateLeadStatus
{
    public class UpdateLeadStatusCommandValidator : AbstractValidator<UpdateLeadStatusCommand>
    {
        public UpdateLeadStatusCommandValidator()
        {
            RuleFor(x => x.LeadId)
                .GreaterThan(0)
                .WithMessage("LeadId must be greater than 0.");

            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("Status is required.")
                .Must(BeAValidStatus)
                .WithMessage($"Status must be a valid LeadStatus.");
        }

        private bool BeAValidStatus(string status)
        {
            return Enum.TryParse(typeof(LeadStatus), status, true, out _);
        }
    }
}
