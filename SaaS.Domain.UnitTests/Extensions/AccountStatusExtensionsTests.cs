using FluentAssertions;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;
using System;
using Xunit;

namespace SaaS.Domain.UnitTests.Extensions
{
    public class AccountStatusExtensionsTests
    {
        [Theory]
        [InlineData(AccountStatus.ACTIVE, "Active")]
        [InlineData(AccountStatus.BUSY, "Busy")]
        [InlineData(AccountStatus.COOLING_DOWN, "CoolingDown")]
        [InlineData(AccountStatus.ACCOUNT_FLAGGED, "AccountFlagged")]
        [InlineData(AccountStatus.BANNED, "Banned")]
        public void ToDbString_ValidEnum_ReturnsExpectedDbString(AccountStatus status, string expectedDbString)
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
            var invalidStatus = (AccountStatus)999;

            // Act
            var act = () => invalidStatus.ToDbString();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("status");
        }

        [Fact]
        public void ToDbString_AllEnumMembers_AreMapped()
        {
            // Ensure every enum member in AccountStatus has an explicit mapping in ToDbString
            foreach (AccountStatus status in Enum.GetValues<AccountStatus>())
            {
                var act = () => status.ToDbString();
                act.Should().NotThrow();
            }
        }

        [Theory]
        [InlineData("Active", AccountStatus.ACTIVE)]
        [InlineData("Busy", AccountStatus.BUSY)]
        [InlineData("CoolingDown", AccountStatus.COOLING_DOWN)]
        [InlineData("AccountFlagged", AccountStatus.ACCOUNT_FLAGGED)]
        [InlineData("Banned", AccountStatus.BANNED)]
        public void ParseFromDb_ValidDbString_ReturnsExpectedEnum(string dbValue, AccountStatus expectedStatus)
        {
            // Act
            var result = dbValue.ParseFromDbToAccountStatus();

            // Assert
            result.Should().Be(expectedStatus);
        }

        [Theory]
        [InlineData("InvalidStatus")]
        [InlineData("ACTIVE")]
        [InlineData("active")]
        [InlineData("cooling_down")]
        [InlineData("")]
        [InlineData("   ")]
        public void ParseFromDb_UnrecognizedString_ThrowsArgumentOutOfRangeException(string invalidDbValue)
        {
            // Act
            var act = () => invalidDbValue.ParseFromDbToAccountStatus();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("dbValue");
        }

        [Fact]
        public void ParseFromDb_NullValue_ThrowsArgumentOutOfRangeException()
        {
            string? nullValue = null;

            // Act
            var act = () => nullValue!.ParseFromDbToAccountStatus();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("dbValue");
        }

        [Theory]
        [InlineData(AccountStatus.ACTIVE)]
        [InlineData(AccountStatus.BUSY)]
        [InlineData(AccountStatus.COOLING_DOWN)]
        [InlineData(AccountStatus.ACCOUNT_FLAGGED)]
        [InlineData(AccountStatus.BANNED)]
        public void RoundTrip_StatusToDbStringAndBack_ReturnsOriginalStatus(AccountStatus originalStatus)
        {
            // Act
            var dbString = originalStatus.ToDbString();
            var parsedStatus = dbString.ParseFromDbToAccountStatus();

            // Assert
            parsedStatus.Should().Be(originalStatus);
        }
    }
}
