using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Features.Auth.Commands.Admin.Login
{
    internal class AdminLoginCommandValidator : AbstractValidator<AdminLoginCommand>
    {
        public AdminLoginCommandValidator()
        {
            RuleFor(x => x.email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

            RuleFor(x => x.password)
                .NotEmpty().WithMessage("Password is required.");

        }
    }
}
