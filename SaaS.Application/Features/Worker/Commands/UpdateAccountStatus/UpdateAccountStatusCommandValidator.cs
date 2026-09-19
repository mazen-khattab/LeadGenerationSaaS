using FluentValidation;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;
using System;

namespace SaaS.Application.Features.Worker.Commands.UpdateAccountStatus
{
    public class UpdateAccountStatusCommandValidator : AbstractValidator<UpdateAccountStatusCommand>
    {
        public UpdateAccountStatusCommandValidator()
        {
            RuleFor(x => x.AccountId)
                .GreaterThan(0)
                .WithMessage("AccountId must be greater than 0.");

            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("Status is required.")
                .Must(BeAValidStatus)
                .WithMessage("Status must be a valid AccountStatus.");
        }

        private bool BeAValidStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            if (Enum.TryParse<AccountStatus>(status, true, out _))
                return true;

            return false;
        }
    }
}
