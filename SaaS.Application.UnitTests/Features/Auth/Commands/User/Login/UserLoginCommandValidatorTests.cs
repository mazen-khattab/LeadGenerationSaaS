using FluentAssertions;
using FluentValidation.TestHelper;
using SaaS.Application.Features.Auth.Commands.User.Login;
using Xunit;

namespace SaaS.Application.UnitTests.Features.Auth.Commands.User.Login
{
    public class UserLoginCommandValidatorTests
    {
        private readonly UserLoginCommandValidator _validator;

        public UserLoginCommandValidatorTests()
        {
            _validator = new UserLoginCommandValidator();
        }

        [Fact]
        public void Validate_WhenEmailAndPasswordAreValid_ShouldNotHaveAnyErrors()
        {
            // Arrange
            var command = new UserLoginCommand("user@example.com", "Password123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_WhenEmailIsEmpty_ShouldHaveValidationErrorForEmail(string? email)
        {
            // Arrange
            var command = new UserLoginCommand(email!, "Password123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.email)
                .WithErrorMessage("Email is required.");
        }

        [Theory]
        [InlineData("notanemail")]
        [InlineData("user@")]
        [InlineData("@example.com")]
        public void Validate_WhenEmailIsInvalidFormat_ShouldHaveValidationErrorForEmail(string email)
        {
            // Arrange
            var command = new UserLoginCommand(email, "Password123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.email)
                .WithErrorMessage("A valid email address is required.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_WhenPasswordIsEmpty_ShouldHaveValidationErrorForPassword(string? password)
        {
            // Arrange
            var command = new UserLoginCommand("user@example.com", password!);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.password)
                .WithErrorMessage("Password is required.");
        }
    }
}
