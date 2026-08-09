using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Features.ConnectedAccounts.Commands.Update
{
    public class UpdateConnectedAccountCommandValidator : AbstractValidator<UpdateConnectedAccountCommand>
    {
        public UpdateConnectedAccountCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Connected account Id is required.")
                .GreaterThan(0)
                .WithMessage("A valid connected account Id must be provided.");

        }
    }
}
