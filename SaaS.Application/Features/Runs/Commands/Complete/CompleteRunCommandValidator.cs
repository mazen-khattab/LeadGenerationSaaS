using System.Collections.Generic;
using FluentValidation;

namespace SaaS.Application.Features.Runs.Commands.Complete
{
    public class CompleteRunCommandValidator : AbstractValidator<CompleteRunCommand>
    {
        public CompleteRunCommandValidator()
        {
            RuleFor(x => x.Leads)
                .NotNull().WithMessage("Leads list must be provided (can be empty).");
        }
    }
}
