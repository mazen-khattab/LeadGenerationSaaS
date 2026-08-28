using FluentValidation;
using System;
using SaaS.Application.Common.Dtos;

namespace SaaS.Application.Features.Worker.Commands.DispatchMessaging
{
    public class DispatchMessagingJobCommandValidator : AbstractValidator<DispatchMessagingJobCommand>
    {
        public DispatchMessagingJobCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEqual(Guid.Empty).WithMessage("A valid user Id must be provided.");

            RuleFor(x => x.BotId)
                .GreaterThan(0).WithMessage("BotId must be greater than zero.");

            RuleFor(x => x.AccountId)
                .GreaterThan(0).WithMessage("AccountId must be greater than zero.");

            RuleFor(x => x.LeadIds)
                .NotNull().WithMessage("LeadIds must be provided.")
                .Must(l => l.Count >= 1).WithMessage("At least one lead id must be provided.")
                .Must(l => l.Count <= 100).WithMessage("A maximum of 100 leads can be dispatched in a single job.");
        }
    }
}
