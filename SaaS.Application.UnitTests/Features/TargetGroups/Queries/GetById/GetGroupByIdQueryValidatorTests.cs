using FluentValidation.TestHelper;
using SaaS.Application.Features.TargetGroups.Queries.GetById;
using Xunit;

namespace SaaS.Application.UnitTests.Features.TargetGroups.Queries.GetById
{
    public class GetGroupByIdQueryValidatorTests
    {
        private readonly GetGroupByIdQueryValidator _validator;

        public GetGroupByIdQueryValidatorTests()
        {
            _validator = new GetGroupByIdQueryValidator();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public void Validate_WhenIdIsGreaterThanZero_ShouldNotHaveValidationError(int validId)
        {
            // Arrange
            var query = new GetGroupByIdQuery(validId);

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Id);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-25)]
        public void Validate_WhenIdIsZeroOrNegative_ShouldHaveValidationError(int invalidId)
        {
            // Arrange
            var query = new GetGroupByIdQuery(invalidId);

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("A valid group Id must be provided.");
        }
    }
}
