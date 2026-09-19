using FluentAssertions;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;
using System;
using Xunit;

namespace SaaS.Domain.UnitTests.Extensions
{
    public class JobTypeExtensionsTests
    {
        [Theory]
        [InlineData(JobType.MESSAGING, "Messaging")]
        public void ToDbString_ValidEnum_ReturnsExpectedDbString(JobType type, string expectedDbString)
        {
            // Act
            var result = type.ToDbString();

            // Assert
            result.Should().Be(expectedDbString);
        }

        [Fact]
        public void ToDbString_UnmappedEnumValue_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var invalidType = (JobType)999;

            // Act
            var act = () => invalidType.ToDbString();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("type");
        }

        [Fact]
        public void ToDbString_AllEnumMembers_AreMapped()
        {
            // Ensure every enum member in JobType has an explicit mapping in ToDbString
            foreach (JobType type in Enum.GetValues<JobType>())
            {
                var act = () => type.ToDbString();
                act.Should().NotThrow();
            }
        }

        [Theory]
        [InlineData("Messaging", JobType.MESSAGING)]
        public void ParseFromDb_ValidDbString_ReturnsExpectedEnum(string dbValue, JobType expectedType)
        {
            // Act
            var result = dbValue.ParseFromDbToJobType();

            // Assert
            result.Should().Be(expectedType);
        }

        [Theory]
        [InlineData("InvalidType")]
        [InlineData("MESSAGING")]
        [InlineData("messaging")]
        [InlineData("")]
        [InlineData("   ")]
        public void ParseFromDb_UnrecognizedString_ThrowsArgumentOutOfRangeException(string invalidDbValue)
        {
            // Act
            var act = () => invalidDbValue.ParseFromDbToJobType();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("dbValue");
        }

        [Fact]
        public void ParseFromDb_NullValue_ThrowsArgumentOutOfRangeException()
        {
            string nullDbValue = null!;

            // Act
            var act = () => nullDbValue.ParseFromDbToJobType();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("dbValue");
        }

        [Theory]
        [InlineData(JobType.MESSAGING)]
        public void RoundTrip_TypeToDbStringAndBack_ReturnsOriginalType(JobType originalType)
        {
            // Act
            var dbString = originalType.ToDbString();
            var parsedType = dbString.ParseFromDbToJobType();

            // Assert
            parsedType.Should().Be(originalType);
        }
    }
}
