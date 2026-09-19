using FluentValidation.TestHelper;
using SaaS.Application.Features.TargetGroups.Queries.GetAll;
using System;
using Xunit;

namespace SaaS.Application.UnitTests.Features.TargetGroups.Queries.GetAll
{
    public class GetAllGroupsQueryValidatorTests
    {
        private readonly GetAllGroupsQueryValidator _validator;

        public GetAllGroupsQueryValidatorTests()
        {
            _validator = new GetAllGroupsQueryValidator();
        }

        [Fact]
        public void Validate_WhenUserIdIsNotEmpty_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetAllGroupsQuery(Guid.NewGuid());

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Validate_WhenUserIdIsEmpty_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetAllGroupsQuery(Guid.Empty);

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId)
                .WithErrorMessage("A valid user Id must be provided.");
        }
    }
}
