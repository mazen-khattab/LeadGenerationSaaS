using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Features.ConnectedAccounts.Queries.GetById
{
    public class GetConnectedAccountByIdQueryValidator : AbstractValidator<GetConnectedAccountByIdQuery>
    {
        public GetConnectedAccountByIdQueryValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Id is required.")
                .GreaterThan(0)
                .WithMessage("A valid connected account Id must be provided.");
        }
    }
}
