using FluentValidation;

namespace SaaS.Application.Features.TargetGroups.Queries.GetAll
{
    public class GetAllGroupsQueryValidator : AbstractValidator<GetAllGroupsQuery>
    {
        public GetAllGroupsQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEqual(Guid.Empty)
                .WithMessage("A valid user Id must be provided.");
        }
    }
}
