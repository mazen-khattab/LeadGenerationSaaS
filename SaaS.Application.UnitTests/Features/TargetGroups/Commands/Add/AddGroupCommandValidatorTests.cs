using FluentValidation.TestHelper;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Features.TargetGroups.Commands.Add;
using System;
using Xunit;

namespace SaaS.Application.UnitTests.Features.TargetGroups.Commands.Add
{
    public class AddGroupCommandValidatorTests
    {
        private readonly AddGroupCommandValidator _validator;

        public AddGroupCommandValidatorTests()
        {
            _validator = new AddGroupCommandValidator();
        }

        [Fact]
        public void Validate_WhenCommandIsValid_ShouldNotHaveAnyValidationErrors()
        {
            // Arrange
            var dto = new AddGroupDto(1, "Dentists", "https://example.com/dentists", "{\"filter\":\"active\"}", true);
            var command = new AddGroupCommand(Guid.NewGuid(), dto);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WhenConfigJsonIsNull_ShouldNotHaveValidationError()
        {
            // Arrange - ConfigJson can be null
            var dto = new AddGroupDto(1, "Dentists", "https://example.com/dentists", null, true);
            var command = new AddGroupCommand(Guid.NewGuid(), dto);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.GroupDto.ConfigJson);
        }

        [Fact]
        public void Validate_WhenUserIdIsEmpty_ShouldHaveValidationError()
        {
            // Arrange
            var dto = new AddGroupDto(1, "Dentists", "https://example.com", "{}", true);
            var command = new AddGroupCommand(Guid.Empty, dto);

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
            var dto = new AddGroupDto(invalidBotId, "Dentists", "https://example.com", "{}", true);
            var command = new AddGroupCommand(Guid.NewGuid(), dto);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.GroupDto.BotId)
                .WithErrorMessage("A valid BotId must be provided.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Validate_WhenGroupNameIsNullOrEmpty_ShouldHaveValidationError(string? invalidName)
        {
            // Arrange
            var dto = new AddGroupDto(1, invalidName!, "https://example.com", "{}", true);
            var command = new AddGroupCommand(Guid.NewGuid(), dto);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.GroupDto.GroupName)
                .WithErrorMessage("GroupName is required.");
        }

        [Theory]
        [InlineData("not a json")]
        [InlineData("{invalid: json")]
        [InlineData("{\"key\": ")]
        public void Validate_WhenConfigJsonIsInvalidJson_ShouldHaveValidationError(string invalidJson)
        {
            // Arrange
            var dto = new AddGroupDto(1, "Dentists", "https://example.com", invalidJson, true);
            var command = new AddGroupCommand(Guid.NewGuid(), dto);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.GroupDto.ConfigJson)
                .WithErrorMessage("InfoJson must be valid JSON.");
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("[]")]
        [InlineData("{\"minFollowers\": 100}")]
        public void Validate_WhenConfigJsonIsValidJson_ShouldNotHaveValidationError(string validJson)
        {
            // Arrange
            var dto = new AddGroupDto(1, "Dentists", "https://example.com", validJson, true);
            var command = new AddGroupCommand(Guid.NewGuid(), dto);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.GroupDto.ConfigJson);
        }
    }
}
