using FluentValidation;

namespace SaaS.Application.Features.Auth.Commands.Admin.RefreshToken
{
    public class AdminRefreshTokenCommandValidator : AbstractValidator<AdminRefreshTokenCommand>
    {
        public AdminRefreshTokenCommandValidator()
        {
            RuleFor(x => x.token)
                .NotEmpty()
                .WithMessage("Refresh token is required.");
        }
    }
}
