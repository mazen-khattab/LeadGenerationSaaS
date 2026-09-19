using FluentAssertions;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;
using System;
using Xunit;

namespace SaaS.Domain.UnitTests.Extensions
{
    public class RunStatusExtensionsTests
    {
        [Theory]
        [InlineData(RunStatus.RUNNING, "Running")]
        [InlineData(RunStatus.PENDING, "Pending")]
        [InlineData(RunStatus.COMPLETED, "Completed")]
        [InlineData(RunStatus.FAILED, "Failed")]
        public void ToDbString_ValidEnum_ReturnsExpectedDbString(RunStatus status, string expectedDbString)
        {
            // Act
            var result = status.ToDbString();

            // Assert
            result.Should().Be(expectedDbString);
        }

        [Fact]
        public void ToDbString_UnmappedEnumValue_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var invalidStatus = (RunStatus)999;

            // Act
            var act = () => invalidStatus.ToDbString();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("status");
        }

        [Fact]
        public void ToDbString_AllEnumMembers_AreMapped()
        {
            // Ensure every enum member in RunStatus has an explicit mapping in ToDbString
            foreach (RunStatus status in Enum.GetValues<RunStatus>())
            {
                var act = () => status.ToDbString();
                act.Should().NotThrow();
            }
        }

        [Theory]
        [InlineData("Running", RunStatus.RUNNING)]
        [InlineData("Pending", RunStatus.PENDING)]
        [InlineData("Completed", RunStatus.COMPLETED)]
        [InlineData("Failed", RunStatus.FAILED)]
        public void ParseFromDb_ValidDbString_ReturnsExpectedEnum(string dbValue, RunStatus expectedStatus)
        {
            // Act
            var result = dbValue.ParseFromDbToRunStatus();

            // Assert
            result.Should().Be(expectedStatus);
        }

        [Theory]
        [InlineData("InvalidStatus")]
        [InlineData("RUNNING")]
        [InlineData("running")]
        [InlineData("completed")]
        [InlineData("")]
        [InlineData("   ")]
        public void ParseFromDb_UnrecognizedString_ThrowsArgumentOutOfRangeException(string invalidDbValue)
        {
            // Act
            var act = () => invalidDbValue.ParseFromDbToRunStatus();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("dbValue");
        }

        [Fact]
        public void ParseFromDb_NullValue_ThrowsArgumentOutOfRangeException()
        {
            string nullDbValue = null!;

            // Act
            var act = () => nullDbValue.ParseFromDbToRunStatus();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("dbValue");
        }

        [Theory]
        [InlineData(RunStatus.RUNNING)]
        [InlineData(RunStatus.PENDING)]
        [InlineData(RunStatus.COMPLETED)]
        [InlineData(RunStatus.FAILED)]
        public void RoundTrip_StatusToDbStringAndBack_ReturnsOriginalStatus(RunStatus originalStatus)
        {
            // Act
            var dbString = originalStatus.ToDbString();
            var parsedStatus = dbString.ParseFromDbToRunStatus();

            // Assert
            parsedStatus.Should().Be(originalStatus);
        }
    }
}
