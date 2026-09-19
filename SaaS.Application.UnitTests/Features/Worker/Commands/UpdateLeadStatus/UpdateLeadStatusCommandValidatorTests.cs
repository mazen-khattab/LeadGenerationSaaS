using FluentValidation.TestHelper;
using SaaS.Application.Features.Worker.Commands.UpdateLeadStatus;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;
using Xunit;

namespace SaaS.Application.UnitTests.Features.Worker.Commands.UpdateLeadStatus
{
    public class UpdateLeadStatusCommandValidatorTests
    {
        private readonly UpdateLeadStatusCommandValidator _validator;

        public UpdateLeadStatusCommandValidatorTests()
        {
            _validator = new UpdateLeadStatusCommandValidator();
        }

        [Fact]
        public void Validate_WhenCommandIsValid_ShouldNotHaveAnyValidationErrors()
        {
            // Arrange
            var command = new UpdateLeadStatusCommand(1, LeadStatus.COMPLETED.ToDbString());

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-50)]
        public void Validate_WhenLeadIdIsZeroOrNegative_ShouldHaveValidationError(long invalidLeadId)
        {
            // Arrange
            var command = new UpdateLeadStatusCommand(invalidLeadId, LeadStatus.COMPLETED.ToDbString());

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.LeadId)
                .WithErrorMessage("LeadId must be greater than 0.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Validate_WhenStatusIsNullOrEmpty_ShouldHaveValidationError(string? invalidStatus)
        {
            // Arrange
            var command = new UpdateLeadStatusCommand(1, invalidStatus!);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Status)
                .WithErrorMessage("Status is required.");
        }

        [Theory]
        [InlineData("InvalidStatus")]
        [InlineData("Unknown")]
        [InlineData("NotAStatus")]
        public void Validate_WhenStatusIsNotValidLeadStatusEnum_ShouldHaveValidationError(string invalidStatus)
        {
            // Arrange
            var command = new UpdateLeadStatusCommand(1, invalidStatus);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Status)
                .WithErrorMessage("Status must be a valid LeadStatus.");
        }

        [Theory]
        [InlineData("Pending")]
        [InlineData("Completed")]
        [InlineData("Failed")]
        [InlineData("pending")]
        [InlineData("COMPLETED")]
        public void Validate_WhenStatusIsValidLeadStatusEnum_ShouldNotHaveValidationError(string validStatus)
        {
            // Arrange
            var command = new UpdateLeadStatusCommand(1, validStatus);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Status);
        }
    }
}
