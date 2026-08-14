using FluentValidation;

namespace SaaS.Application.Features.Leads.Queries.GetAllowedPendingLeads
{
    public class GetAllowedPendingLeadsQueryValidator : AbstractValidator<GetAllowedPendingLeadsQuery>
    {
        public GetAllowedPendingLeadsQueryValidator()
        {
            RuleFor(x => x.BotId)
                .GreaterThan(0)
                .WithMessage("BotId must be greater than 0.");
        }
    }
}
