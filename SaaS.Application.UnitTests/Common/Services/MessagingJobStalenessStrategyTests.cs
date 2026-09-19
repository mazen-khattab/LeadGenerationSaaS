using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaaS.Application.Common.Services;
using SaaS.Domain.Entities;
using SaaS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace SaaS.Application.UnitTests.Common.Services
{
    public class MessagingJobStalenessStrategyTests
    {
        private readonly MessagingJobStalenessStrategy _strategy;
        private readonly Mock<ILogger> _loggerMock;

        public MessagingJobStalenessStrategyTests()
        {
            _strategy = new MessagingJobStalenessStrategy();
            _loggerMock = new Mock<ILogger>();
        }

        [Fact]
        public void JobType_ShouldBeMessaging()
        {
            // Assert
            _strategy.JobType.Should().Be(JobType.MESSAGING);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ExtractLeadIds_WhenPayloadIsNullOrWhiteSpace_ReturnsEmpty(string? payloadJson)
        {
            // Arrange
            var job = new Job { PayloadJson = payloadJson! };

            // Act
            var result = _strategy.ExtractLeadIds(job, _loggerMock.Object);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ExtractLeadIds_WhenPayloadContainsLeadIdsArray_ReturnsParsedIds()
        {
            // Arrange
            var job = new Job
            {
                PayloadJson = JsonSerializer.Serialize(new { leadIds = new long[] { 101, 202, 303 } })
            };

            // Act
            var result = _strategy.ExtractLeadIds(job, _loggerMock.Object);

            // Assert
            result.Should().Equal(101L, 202L, 303L);
        }

        [Fact]
        public void ExtractLeadIds_WhenLeadIdsArrayIsEmpty_ReturnsEmpty()
        {
            // Arrange
            var job = new Job
            {
                PayloadJson = JsonSerializer.Serialize(new { leadIds = Array.Empty<long>() })
            };

            // Act
            var result = _strategy.ExtractLeadIds(job, _loggerMock.Object);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ExtractLeadIds_WhenPayloadMissingLeadIdsProperty_ReturnsEmpty()
        {
            // Arrange
            var job = new Job
            {
                PayloadJson = JsonSerializer.Serialize(new { message = "hello", count = 5 })
            };

            // Act
            var result = _strategy.ExtractLeadIds(job, _loggerMock.Object);

            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData("{\"leadIds\": 12345}")]
        [InlineData("{\"leadIds\": \"not-an-array\"}")]
        [InlineData("{\"leadIds\": true}")]
        [InlineData("{\"leadIds\": {\"id\": 1}}")]
        public void ExtractLeadIds_WhenLeadIdsPropertyIsNotAnArray_ReturnsEmpty(string payloadJson)
        {
            // Arrange
            var job = new Job { PayloadJson = payloadJson };

            // Act
            var result = _strategy.ExtractLeadIds(job, _loggerMock.Object);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ExtractLeadIds_WhenPayloadIsInvalidJson_ThrowsJsonException()
        {
            // Arrange
            var job = new Job { PayloadJson = "{ this is invalid json }" };

            // Act
            var act = () => _strategy.ExtractLeadIds(job, _loggerMock.Object);

            // Assert
            act.Should().Throw<JsonException>();
        }

        [Fact]
        public void GetLastActivity_WhenLeadIdsIsEmpty_ReturnsJobCreatedAt()
        {
            // Arrange
            var createdAt = new DateTime(2026, 9, 11, 10, 0, 0, DateTimeKind.Utc);
            var job = new Job { CreatedAt = createdAt };
            var leadIds = Array.Empty<long>();
            var lookup = new Dictionary<long, DateTime>();

            // Act
            var result = _strategy.GetLastActivity(job, leadIds, lookup);

            // Assert
            result.Should().Be(createdAt);
        }

        [Fact]
        public void GetLastActivity_WhenLeadIdsNotInLookup_ReturnsJobCreatedAt()
        {
            // Arrange
            var createdAt = new DateTime(2026, 9, 11, 10, 0, 0, DateTimeKind.Utc);
            var job = new Job { CreatedAt = createdAt };
            var leadIds = new long[] { 1, 2, 3 };
            var lookup = new Dictionary<long, DateTime>
            {
                { 999, createdAt.AddHours(2) } // Unrelated ID
            };

            // Act
            var result = _strategy.GetLastActivity(job, leadIds, lookup);

            // Assert
            result.Should().Be(createdAt);
        }

        [Fact]
        public void GetLastActivity_WhenProcessedAtIsOlderThanCreatedAt_ReturnsJobCreatedAt()
        {
            // Arrange
            var createdAt = new DateTime(2026, 9, 11, 10, 0, 0, DateTimeKind.Utc);
            var job = new Job { CreatedAt = createdAt };
            var leadIds = new long[] { 1 };
            var lookup = new Dictionary<long, DateTime>
            {
                { 1, createdAt.AddMinutes(-30) } // Processed before job creation
            };

            // Act
            var result = _strategy.GetLastActivity(job, leadIds, lookup);

            // Assert
            result.Should().Be(createdAt);
        }

        [Fact]
        public void GetLastActivity_WhenProcessedAtIsNewerThanCreatedAt_ReturnsProcessedAt()
        {
            // Arrange
            var createdAt = new DateTime(2026, 9, 11, 10, 0, 0, DateTimeKind.Utc);
            var expectedActivity = createdAt.AddMinutes(45);
            var job = new Job { CreatedAt = createdAt };
            var leadIds = new long[] { 1 };
            var lookup = new Dictionary<long, DateTime>
            {
                { 1, expectedActivity }
            };

            // Act
            var result = _strategy.GetLastActivity(job, leadIds, lookup);

            // Assert
            result.Should().Be(expectedActivity);
        }

        [Fact]
        public void GetLastActivity_WhenMultipleLeadsProcessed_ReturnsLatestProcessedAt()
        {
            // Arrange
            var createdAt = new DateTime(2026, 9, 11, 10, 0, 0, DateTimeKind.Utc);
            var latestProcessedAt = createdAt.AddMinutes(90);
            var job = new Job { CreatedAt = createdAt };
            var leadIds = new long[] { 1, 2, 3, 4 };
            var lookup = new Dictionary<long, DateTime>
            {
                { 1, createdAt.AddMinutes(15) },
                { 2, latestProcessedAt },
                { 3, createdAt.AddMinutes(45) }
                // 4 is not in lookup
            };

            // Act
            var result = _strategy.GetLastActivity(job, leadIds, lookup);

            // Assert
            result.Should().Be(latestProcessedAt);
        }
    }
}
