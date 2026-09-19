using FluentAssertions;
using FluentValidation.TestHelper;
using SaaS.Application.Features.Auth.Commands.Admin.Login;
using Xunit;

namespace SaaS.Application.UnitTests.Features.Auth.Commands.Admin.Login
{
    public class AdminLoginCommandValidatorTests
    {
        private readonly AdminLoginCommandValidator _validator;

        public AdminLoginCommandValidatorTests()
        {
            _validator = new AdminLoginCommandValidator();
        }

        [Fact]
        public void Validate_WhenEmailAndPasswordAreValid_ShouldNotHaveAnyErrors()
        {
            // Arrange
            var command = new AdminLoginCommand("admin@example.com", "SecurePassword123!");

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
            var command = new AdminLoginCommand(email!, "SecurePassword123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.email)
                .WithErrorMessage("Email is required.");
        }

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("admin@")]
        [InlineData("@example.com")]
        [InlineData("not-an-email-at-all")]
        public void Validate_WhenEmailIsInvalidFormat_ShouldHaveValidationErrorForEmail(string email)
        {
            // Arrange
            var command = new AdminLoginCommand(email, "SecurePassword123!");

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
            var command = new AdminLoginCommand("admin@example.com", password!);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.password)
                .WithErrorMessage("Password is required.");
        }

        [Fact]
        public void Validate_WhenBothEmailAndPasswordAreEmpty_ShouldHaveValidationErrorsForBoth()
        {
            // Arrange
            var command = new AdminLoginCommand("", "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.email);
            result.ShouldHaveValidationErrorFor(x => x.password);
        }
    }
}
