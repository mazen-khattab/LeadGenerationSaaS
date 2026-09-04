using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Settings;
using SaaS.Application.Features.ConnectedAccounts.Commands.ProcessCooldown;
using SaaS.Domain.Entities;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

using SaaS.Application.UnitTests.Common;

namespace SaaS.Application.UnitTests.Features.ConnectedAccounts.Commands.ProcessCooldown
{

    public class ProcessAccountCooldownCommandHandlerTests
    {
        private readonly Mock<IOptionsMonitor<GeneralSettings>> _optionsMock;
        private readonly Mock<ILogger<ProcessAccountCooldownCommandHandler>> _loggerMock;
        private readonly GeneralSettings _generalSettings;

        public ProcessAccountCooldownCommandHandlerTests()
        {
            _optionsMock = new Mock<IOptionsMonitor<GeneralSettings>>();
            _loggerMock = new Mock<ILogger<ProcessAccountCooldownCommandHandler>>();

            // Setup a default cooldown of 1 day
            _generalSettings = new GeneralSettings { AccountCooldownperiodDays = 1 };
            _optionsMock.Setup(o => o.CurrentValue).Returns(_generalSettings);
        }

        private MockAppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<MockAppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new MockAppDbContext(options);
        }

        [Fact]
        public async Task Should_Reactivate_Accounts_When_Cooldown_Period_Has_Expired()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            
            var account = new ConnectedAccount
            {
                DisplayName = "Expired Account",
                Platform = "Facebook",
                Status = AccountStatus.COOLING_DOWN.ToDbString(),
                LastStatusUpdatedAt = DateTime.UtcNow.AddDays(-2) // 2 days old, strictly older than 1 day
            };

            await dbContext.ConnectedAccounts.AddAsync(account);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new ProcessAccountCooldownCommandHandler(dbContext, _optionsMock.Object, _loggerMock.Object);
            var command = new ProcessAccountCooldownCommand();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(1); // 1 account reactivated

            var updatedAccount = await dbContext.ConnectedAccounts.FindAsync(account.Id);
            updatedAccount.Should().NotBeNull();
            updatedAccount!.Status.Should().Be(AccountStatus.ACTIVE.ToDbString());
            updatedAccount.LastStatusUpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Should_Not_Reactivate_Accounts_When_Cooldown_Period_Is_Not_Expired()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            
            var account = new ConnectedAccount
            {
                DisplayName = "Recent Account",
                Platform = "Facebook",
                Status = AccountStatus.COOLING_DOWN.ToDbString(),
                LastStatusUpdatedAt = DateTime.UtcNow.AddHours(-12) // 12 hours old, within the 1-day threshold
            };

            await dbContext.ConnectedAccounts.AddAsync(account);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new ProcessAccountCooldownCommandHandler(dbContext, _optionsMock.Object, _loggerMock.Object);
            var command = new ProcessAccountCooldownCommand();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(0); // 0 accounts reactivated

            var unmodifiedAccount = await dbContext.ConnectedAccounts.FindAsync(account.Id);
            unmodifiedAccount.Should().NotBeNull();
            unmodifiedAccount!.Status.Should().Be(AccountStatus.COOLING_DOWN.ToDbString());
        }
    }
}
