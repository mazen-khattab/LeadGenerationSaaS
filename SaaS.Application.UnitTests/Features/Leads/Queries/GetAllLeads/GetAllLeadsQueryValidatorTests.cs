using FluentValidation.TestHelper;
using SaaS.Application.Features.Leads.Queries.GetAllLeads;
using System;
using Xunit;

namespace SaaS.Application.UnitTests.Features.Leads.Queries.GetAllLeads
{
    public class GetAllLeadsQueryValidatorTests
    {
        private readonly GetAllLeadsQueryValidator _validator;

        public GetAllLeadsQueryValidatorTests()
        {
            _validator = new GetAllLeadsQueryValidator();
        }

        [Fact]
        public void Validate_WhenQueryIsValid_ShouldNotHaveAnyValidationErrors()
        {
            // Arrange
            var query = new GetAllLeadsQuery(Guid.NewGuid(), 1, 1, 10, null);

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-50)]
        public void Validate_WhenBotIdIsZeroOrNegative_ShouldHaveValidationError(int invalidBotId)
        {
            // Arrange
            var query = new GetAllLeadsQuery(Guid.NewGuid(), invalidBotId, 1, 10, null);

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.BotId)
                .WithErrorMessage("BotId must be greater than 0.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public void Validate_WhenPageNumberIsLessThanOne_ShouldHaveValidationError(int invalidPageNumber)
        {
            // Arrange
            var query = new GetAllLeadsQuery(Guid.NewGuid(), 1, invalidPageNumber, 10, null);

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.PageNumber)
                .WithErrorMessage("PageNumber must be at least 1.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-20)]
        public void Validate_WhenPageSizeIsLessThanOne_ShouldHaveValidationError(int invalidPageSize)
        {
            // Arrange
            var query = new GetAllLeadsQuery(Guid.NewGuid(), 1, 1, invalidPageSize, null);

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.PageSize)
                .WithErrorMessage("PageSize must be between 1 and 100.");
        }

        [Theory]
        [InlineData(101)]
        [InlineData(200)]
        public void Validate_WhenPageSizeExceedsOneHundred_ShouldHaveValidationError(int invalidPageSize)
        {
            // Arrange
            var query = new GetAllLeadsQuery(Guid.NewGuid(), 1, 1, invalidPageSize, null);

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.PageSize)
                .WithErrorMessage("PageSize must be between 1 and 100.");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(100)]
        public void Validate_WhenPageSizeIsWithinValidRange_ShouldNotHaveValidationError(int validPageSize)
        {
            // Arrange
            var query = new GetAllLeadsQuery(Guid.NewGuid(), 1, 1, validPageSize, null);

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(100)]
        public void Validate_WhenPageNumberIsGreaterThanOrEqualToOne_ShouldNotHaveValidationError(int validPageNumber)
        {
            // Arrange
            var query = new GetAllLeadsQuery(Guid.NewGuid(), 1, validPageNumber, 10, null);

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.PageNumber);
        }
    }
}
