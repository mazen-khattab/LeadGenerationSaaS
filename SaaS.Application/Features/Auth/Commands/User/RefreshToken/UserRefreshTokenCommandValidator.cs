using FluentValidation;

namespace SaaS.Application.Features.Auth.Commands.User.RefreshToken
{
    public class UserRefreshTokenCommandValidator : AbstractValidator<UserRefreshTokenCommand>
    {
        public UserRefreshTokenCommandValidator()
        {
            RuleFor(x => x.token)
                .NotEmpty()
                .WithMessage("Refresh token is required.");
        }
    }
}
