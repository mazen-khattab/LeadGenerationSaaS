using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Features.ConnectedAccounts.Queries.GetAll
{
    public class GetAllConnectedAccountsQueryValidator : AbstractValidator<GetAllConnectedAccountsQuery>
    {
        public GetAllConnectedAccountsQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEqual(Guid.Empty)
                .WithMessage("A valid user Id must be provided.");
        }
    }

}
