using FluentValidation;
using SaaS.Application.Common.Dtos;

namespace SaaS.Application.Features.Auth.Commands.User.Login
{
    public class UserLoginCommandValidator : AbstractValidator<UserLoginCommand>
    {
        public UserLoginCommandValidator()
        {
            RuleFor(x => x.email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");

            RuleFor(x => x.password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }
}
