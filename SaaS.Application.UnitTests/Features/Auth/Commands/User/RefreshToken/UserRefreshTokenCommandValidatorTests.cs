using FluentAssertions;
using FluentValidation.TestHelper;
using SaaS.Application.Features.Auth.Commands.User.RefreshToken;
using Xunit;

namespace SaaS.Application.UnitTests.Features.Auth.Commands.User.RefreshToken
{
    public class UserRefreshTokenCommandValidatorTests
    {
        private readonly UserRefreshTokenCommandValidator _validator;

        public UserRefreshTokenCommandValidatorTests()
        {
            _validator = new UserRefreshTokenCommandValidator();
        }

        [Fact]
        public void Validate_WhenTokenIsValid_ShouldNotHaveAnyErrors()
        {
            // Arrange
            var command = new UserRefreshTokenCommand("valid-token-user-12345");

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
            var command = new UserRefreshTokenCommand(token!);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.token)
                .WithErrorMessage("Refresh token is required.");
        }
    }
}
