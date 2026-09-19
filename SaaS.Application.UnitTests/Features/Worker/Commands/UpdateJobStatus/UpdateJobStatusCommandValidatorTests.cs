using FluentValidation.TestHelper;
using SaaS.Application.Features.Worker.Commands.UpdateJobStatus;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;
using Xunit;

namespace SaaS.Application.UnitTests.Features.Worker.Commands.UpdateJobStatus
{
    public class UpdateJobStatusCommandValidatorTests
    {
        private readonly UpdateJobStatusCommandValidator _validator;

        public UpdateJobStatusCommandValidatorTests()
        {
            _validator = new UpdateJobStatusCommandValidator();
        }

        [Fact]
        public void Validate_WhenCommandIsValid_ShouldNotHaveAnyValidationErrors()
        {
            // Arrange
            var command = new UpdateJobStatusCommand(1, JobStatus.COMPLETED.ToDbString());

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Validate_WhenJobIdIsZeroOrNegative_ShouldHaveValidationError(long invalidJobId)
        {
            // Arrange
            var command = new UpdateJobStatusCommand(invalidJobId, JobStatus.COMPLETED.ToDbString());

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.JobId)
                .WithErrorMessage("JobId must be greater than 0.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Validate_WhenStatusIsNullOrEmpty_ShouldHaveValidationError(string? invalidStatus)
        {
            // Arrange
            var command = new UpdateJobStatusCommand(1, invalidStatus!);

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
        public void Validate_WhenStatusIsNotValidJobStatusEnum_ShouldHaveValidationError(string invalidStatus)
        {
            // Arrange
            var command = new UpdateJobStatusCommand(1, invalidStatus);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Status)
                .WithErrorMessage("Status must be a valid JobStatus.");
        }

        [Theory]
        [InlineData("Pending")]
        [InlineData("Processing")]
        [InlineData("Completed")]
        [InlineData("Failed")]
        [InlineData("pending")]
        [InlineData("COMPLETED")]
        public void Validate_WhenStatusIsValidJobStatusEnum_ShouldNotHaveValidationError(string validStatus)
        {
            // Arrange
            var command = new UpdateJobStatusCommand(1, validStatus);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Status);
        }
    }
}
