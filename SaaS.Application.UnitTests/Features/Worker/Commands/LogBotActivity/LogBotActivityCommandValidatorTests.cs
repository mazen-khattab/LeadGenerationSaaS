using FluentValidation.TestHelper;
using SaaS.Application.Features.Worker.Commands.LogBotActivity;
using System;
using Xunit;

namespace SaaS.Application.UnitTests.Features.Worker.Commands.LogBotActivity
{
    public class LogBotActivityCommandValidatorTests
    {
        private readonly LogBotActivityCommandValidator _validator;

        public LogBotActivityCommandValidatorTests()
        {
            _validator = new LogBotActivityCommandValidator();
        }

        [Fact]
        public void Validate_WhenCommandIsValid_ShouldNotHaveAnyValidationErrors()
        {
            // Arrange
            var command = new LogBotActivityCommand
            {
                CorrelationId = "corr-123",
                UserId = Guid.NewGuid(),
                LogLevel = "INFO",
                Message = "Started job processing"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Validate_WhenCorrelationIdIsNullOrEmpty_ShouldHaveValidationError(string? invalidCorrelationId)
        {
            // Arrange
            var command = new LogBotActivityCommand
            {
                CorrelationId = invalidCorrelationId!,
                UserId = Guid.NewGuid(),
                LogLevel = "INFO",
                Message = "Valid message"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.CorrelationId)
                .WithErrorMessage("CorrelationId is required.");
        }

        [Fact]
        public void Validate_WhenUserIdIsEmpty_ShouldHaveValidationError()
        {
            // Arrange
            var command = new LogBotActivityCommand
            {
                CorrelationId = "corr-123",
                UserId = Guid.Empty,
                LogLevel = "INFO",
                Message = "Valid message"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId)
                .WithErrorMessage("A valid user Id must be provided.");
        }

        [Theory]
        [InlineData("INFO")]
        [InlineData("WARN")]
        [InlineData("ERROR")]
        public void Validate_WhenLogLevelIsValid_ShouldNotHaveValidationError(string validLevel)
        {
            // Arrange
            var command = new LogBotActivityCommand
            {
                CorrelationId = "corr-123",
                UserId = Guid.NewGuid(),
                LogLevel = validLevel,
                Message = "Valid message"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.LogLevel);
        }

        [Theory]
        [InlineData("DEBUG")]
        [InlineData("FATAL")]
        [InlineData("info")] // Case sensitive check in validator: level == "INFO" || level == "WARN" || level == "ERROR"
        [InlineData("TRACE")]
        public void Validate_WhenLogLevelIsInvalid_ShouldHaveValidationError(string invalidLevel)
        {
            // Arrange
            var command = new LogBotActivityCommand
            {
                CorrelationId = "corr-123",
                UserId = Guid.NewGuid(),
                LogLevel = invalidLevel,
                Message = "Valid message"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.LogLevel)
                .WithErrorMessage("LogLevel must be 'INFO', 'WARN', or 'ERROR'.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Validate_WhenMessageIsNullOrEmpty_ShouldHaveValidationError(string? invalidMessage)
        {
            // Arrange
            var command = new LogBotActivityCommand
            {
                CorrelationId = "corr-123",
                UserId = Guid.NewGuid(),
                LogLevel = "INFO",
                Message = invalidMessage!
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Message)
                .WithErrorMessage("Message is required.");
        }
    }
}
