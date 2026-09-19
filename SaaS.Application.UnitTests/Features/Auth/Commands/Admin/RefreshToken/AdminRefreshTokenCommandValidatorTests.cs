using FluentAssertions;
using FluentValidation.TestHelper;
using SaaS.Application.Features.Auth.Commands.Admin.RefreshToken;
using Xunit;

namespace SaaS.Application.UnitTests.Features.Auth.Commands.Admin.RefreshToken
{
    public class AdminRefreshTokenCommandValidatorTests
    {
        private readonly AdminRefreshTokenCommandValidator _validator;

        public AdminRefreshTokenCommandValidatorTests()
        {
            _validator = new AdminRefreshTokenCommandValidator();
        }

        [Fact]
        public void Validate_WhenTokenIsValid_ShouldNotHaveAnyErrors()
        {
            // Arrange
            var command = new AdminRefreshTokenCommand("valid-refresh-token-12345");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_WhenTokenIsEmptyOrNull_ShouldHaveValidationError(string? token)
        {
            // Arrange
            var command = new AdminRefreshTokenCommand(token!);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.token)
                .WithErrorMessage("Refresh token is required.");
        }
    }
}
