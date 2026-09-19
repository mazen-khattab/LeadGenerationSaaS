using FluentValidation.TestHelper;
using SaaS.Application.Features.Leads.Queries.GetAllowedPendingLeads;
using System;
using Xunit;

namespace SaaS.Application.UnitTests.Features.Leads.Queries.GetAllowedPendingLeads
{
    public class GetAllowedPendingLeadsQueryValidatorTests
    {
        private readonly GetAllowedPendingLeadsQueryValidator _validator;

        public GetAllowedPendingLeadsQueryValidatorTests()
        {
            _validator = new GetAllowedPendingLeadsQueryValidator();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public void Validate_WhenBotIdIsGreaterThanZero_ShouldNotHaveAnyValidationErrors(int validBotId)
        {
            // Arrange
            var query = new GetAllowedPendingLeadsQuery(Guid.NewGuid(), validBotId);

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
            var query = new GetAllowedPendingLeadsQuery(Guid.NewGuid(), invalidBotId);

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.BotId)
                .WithErrorMessage("BotId must be greater than 0.");
        }
    }
}
