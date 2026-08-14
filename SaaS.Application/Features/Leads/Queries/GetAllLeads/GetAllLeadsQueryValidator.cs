using FluentValidation;

namespace SaaS.Application.Features.Leads.Queries.GetAllLeads
{
    public class GetAllLeadsQueryValidator : AbstractValidator<GetAllLeadsQuery>
    {
        public GetAllLeadsQueryValidator()
        {
            RuleFor(x => x.BotId)
                .GreaterThan(0)
                .WithMessage("BotId must be greater than 0.");

            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("PageNumber must be at least 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("PageSize must be between 1 and 100.");
        }
    }
}
