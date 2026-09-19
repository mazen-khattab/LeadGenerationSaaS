using FluentAssertions;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;
using System;
using Xunit;

namespace SaaS.Domain.UnitTests.Extensions
{
    public class LeadStatusExtensionsTests
    {
        [Theory]
        [InlineData(LeadStatus.PENDING, "Pending")]
        [InlineData(LeadStatus.COMPLETED, "Completed")]
        [InlineData(LeadStatus.FAILED, "Failed")]
        public void ToDbString_ValidEnum_ReturnsExpectedDbString(LeadStatus status, string expectedDbString)
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
            var invalidStatus = (LeadStatus)999;

            // Act
            var act = () => invalidStatus.ToDbString();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("status");
        }

        [Fact]
        public void ToDbString_AllEnumMembers_AreMapped()
        {
            // Ensure every enum member in LeadStatus has an explicit mapping in ToDbString
            foreach (LeadStatus status in Enum.GetValues<LeadStatus>())
            {
                var act = () => status.ToDbString();
                act.Should().NotThrow();
            }
        }

        [Theory]
        [InlineData("Pending", LeadStatus.PENDING)]
        [InlineData("Completed", LeadStatus.COMPLETED)]
        [InlineData("Failed", LeadStatus.FAILED)]
        public void ParseFromDb_ValidDbString_ReturnsExpectedEnum(string dbValue, LeadStatus expectedStatus)
        {
            // Act
            var result = dbValue.ParseFromDbToLeadStatus();

            // Assert
            result.Should().Be(expectedStatus);
        }

        [Theory]
        [InlineData("InvalidStatus")]
        [InlineData("PENDING")]
        [InlineData("pending")]
        [InlineData("completed")]
        [InlineData("")]
        [InlineData("   ")]
        public void ParseFromDb_UnrecognizedString_ThrowsArgumentOutOfRangeException(string invalidDbValue)
        {
            // Act
            var act = () => invalidDbValue.ParseFromDbToLeadStatus();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("dbValue");
        }

        [Fact]
        public void ParseFromDb_NullValue_ThrowsArgumentOutOfRangeException()
        {
            string nullDbValue = null!;

            // Act
            var act = () => nullDbValue.ParseFromDbToLeadStatus();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("dbValue");
        }

        [Theory]
        [InlineData(LeadStatus.PENDING)]
        [InlineData(LeadStatus.COMPLETED)]
        [InlineData(LeadStatus.FAILED)]
        public void RoundTrip_StatusToDbStringAndBack_ReturnsOriginalStatus(LeadStatus originalStatus)
        {
            // Act
            var dbString = originalStatus.ToDbString();
            var parsedStatus = dbString.ParseFromDbToLeadStatus();

            // Assert
            parsedStatus.Should().Be(originalStatus);
        }
    }
}
