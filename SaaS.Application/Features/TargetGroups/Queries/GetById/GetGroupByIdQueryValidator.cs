using FluentValidation;

namespace SaaS.Application.Features.TargetGroups.Queries.GetById
{
    public class GetGroupByIdQueryValidator : AbstractValidator<GetGroupByIdQuery>
    {
        public GetGroupByIdQueryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("A valid group Id must be provided.");
        }
    }
}
