using FluentValidation.TestHelper;
using SaaS.Application.Features.Worker.Commands.DispatchMessaging;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SaaS.Application.UnitTests.Features.Worker.Commands.DispatchMessaging
{
    public class DispatchMessagingJobCommandValidatorTests
    {
        private readonly DispatchMessagingJobCommandValidator _validator;

        public DispatchMessagingJobCommandValidatorTests()
        {
            _validator = new DispatchMessagingJobCommandValidator();
        }

        [Fact]
        public void Validate_WhenCommandIsValid_ShouldNotHaveAnyValidationErrors()
        {
            // Arrange
            var command = new DispatchMessagingJobCommand(Guid.NewGuid(), 1, 10, new List<long> { 1, 2, 3 });

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WhenUserIdIsEmpty_ShouldHaveValidationError()
        {
            // Arrange
            var command = new DispatchMessagingJobCommand(Guid.Empty, 1, 10, new List<long> { 1 });

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId)
                .WithErrorMessage("A valid user Id must be provided.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public void Validate_WhenBotIdIsZeroOrNegative_ShouldHaveValidationError(int invalidBotId)
        {
            // Arrange
            var command = new DispatchMessagingJobCommand(Guid.NewGuid(), invalidBotId, 10, new List<long> { 1 });

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.BotId)
                .WithErrorMessage("BotId must be greater than zero.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-5)]
        public void Validate_WhenAccountIdIsZeroOrNegative_ShouldHaveValidationError(int invalidAccountId)
        {
            // Arrange
            var command = new DispatchMessagingJobCommand(Guid.NewGuid(), 1, invalidAccountId, new List<long> { 1 });

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.AccountId)
                .WithErrorMessage("AccountId must be greater than zero.");
        }

        [Fact]
        public void Validate_WhenLeadIdsIsNull_ShouldHaveValidationError()
        {
            // Arrange
            var command = new DispatchMessagingJobCommand(Guid.NewGuid(), 1, 10, null!);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.LeadIds)
                .WithErrorMessage("LeadIds must be provided.");
        }

        [Fact]
        public void Validate_WhenLeadIdsIsEmpty_ShouldHaveValidationError()
        {
            // Arrange
            var command = new DispatchMessagingJobCommand(Guid.NewGuid(), 1, 10, new List<long>());

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.LeadIds)
                .WithErrorMessage("At least one lead id must be provided.");
        }

        [Fact]
        public void Validate_WhenLeadIdsExceedsOneHundred_ShouldHaveValidationError()
        {
            // Arrange - 101 leads
            var leadIds = Enumerable.Range(1, 101).Select(i => (long)i).ToList();
            var command = new DispatchMessagingJobCommand(Guid.NewGuid(), 1, 10, leadIds);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.LeadIds)
                .WithErrorMessage("A maximum of 100 leads can be dispatched in a single job.");
        }

        [Fact]
        public void Validate_WhenLeadIdsHasOneHundredElements_ShouldNotHaveValidationError()
        {
            // Arrange - exactly 100 leads (boundary)
            var leadIds = Enumerable.Range(1, 100).Select(i => (long)i).ToList();
            var command = new DispatchMessagingJobCommand(Guid.NewGuid(), 1, 10, leadIds);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.LeadIds);
        }
    }
}
