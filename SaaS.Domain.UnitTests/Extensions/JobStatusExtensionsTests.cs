using FluentAssertions;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;
using System;
using Xunit;

namespace SaaS.Domain.UnitTests.Extensions
{
    public class JobStatusExtensionsTests
    {
        [Theory]
        [InlineData(JobStatus.PENDING, "Pending")]
        [InlineData(JobStatus.PROCESSING, "Processing")]
        [InlineData(JobStatus.COMPLETED, "Completed")]
        [InlineData(JobStatus.FAILED, "Failed")]
        public void ToDbString_ValidEnum_ReturnsExpectedDbString(JobStatus status, string expectedDbString)
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
            var invalidStatus = (JobStatus)999;

            // Act
            var act = () => invalidStatus.ToDbString();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("status");
        }

        [Fact]
        public void ToDbString_AllEnumMembers_AreMapped()
        {
            // Ensure every enum member in JobStatus has an explicit mapping in ToDbString
            foreach (JobStatus status in Enum.GetValues<JobStatus>())
            {
                var act = () => status.ToDbString();
                act.Should().NotThrow();
            }
        }

        [Theory]
        [InlineData("Pending", JobStatus.PENDING)]
        [InlineData("Processing", JobStatus.PROCESSING)]
        [InlineData("Completed", JobStatus.COMPLETED)]
        [InlineData("Failed", JobStatus.FAILED)]
        public void ParseFromDb_ValidDbString_ReturnsExpectedEnum(string dbValue, JobStatus expectedStatus)
        {
            // Act (both extension and static invocation)
            var resultViaExtension = dbValue.ParseFromDbToJobStatus();

            // Assert
            resultViaExtension.Should().Be(expectedStatus);
        }

        [Theory]
        [InlineData("InvalidStatus")]
        [InlineData("PENDING")]
        [InlineData("pending")]
        [InlineData("PROCESSING")]
        [InlineData("")]
        [InlineData("   ")]
        public void ParseFromDb_UnrecognizedString_ThrowsArgumentOutOfRangeException(string invalidDbValue)
        {
            // Act
            var act = () => invalidDbValue.ParseFromDbToJobStatus();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("dbValue");
        }

        [Fact]
        public void ParseFromDb_NullValue_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            string? nullValue = null;

            // Act
            var act = () => nullValue!.ParseFromDbToJobStatus();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("dbValue");
        }

        [Theory]
        [InlineData(JobStatus.PENDING)]
        [InlineData(JobStatus.PROCESSING)]
        [InlineData(JobStatus.COMPLETED)]
        [InlineData(JobStatus.FAILED)]
        public void RoundTrip_StatusToDbStringAndBack_ReturnsOriginalStatus(JobStatus originalStatus)
        {
            // Act
            var dbString = originalStatus.ToDbString();
            var parsedStatus = dbString.ParseFromDbToJobStatus();

            // Assert
            parsedStatus.Should().Be(originalStatus);
        }
    }
}
