using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SaaS.Application.Common.Models;
using SaaS.Application.Features.Worker.Commands.UpdateAccountStatus;
using SaaS.Application.UnitTests.Common;
using SaaS.Domain.Entities;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SaaS.Application.UnitTests.Features.Worker.Commands.UpdateAccountStatus
{
    public class UpdateAccountStatusCommandHandlerTests
    {
        private readonly Mock<ILogger<UpdateAccountStatusCommandHandler>> _loggerMock;

        public UpdateAccountStatusCommandHandlerTests()
        {
            _loggerMock = new Mock<ILogger<UpdateAccountStatusCommandHandler>>();
        }

        private MockAppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<MockAppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new MockAppDbContext(options);
        }

        [Fact]
        public async Task Should_Return_NotFound_When_Account_Does_Not_Exist()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var handler = new UpdateAccountStatusCommandHandler(dbContext, _loggerMock.Object);
            var command = new UpdateAccountStatusCommand(999, "Active");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ErrorType.NotFound);
            result.Message.Should().Be("Account not found.");
        }

        [Fact]
        public async Task Should_Return_ValidationError_When_Status_Is_Invalid()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var account = new ConnectedAccount
            {
                DisplayName = "Test Account",
                Platform = "Facebook",
                Status = AccountStatus.ACTIVE.ToDbString()
            };
            await dbContext.ConnectedAccounts.AddAsync(account);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new UpdateAccountStatusCommandHandler(dbContext, _loggerMock.Object);
            var command = new UpdateAccountStatusCommand(account.Id, "WRONG_VALUE");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ErrorType.ValidationError);
            result.Message.Should().Be("Invalid account status.");
        }

        [Fact]
        public async Task Should_Update_Account_Status_And_Timestamp_Successfully()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var account = new ConnectedAccount
            {
                DisplayName = "Test Account",
                Platform = "Facebook",
                Status = AccountStatus.ACTIVE.ToDbString(),
                LastStatusUpdatedAt = DateTime.UtcNow.AddDays(-1)
            };
            await dbContext.ConnectedAccounts.AddAsync(account);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new UpdateAccountStatusCommandHandler(dbContext, _loggerMock.Object);
            var command = new UpdateAccountStatusCommand(account.Id, "COOLING_DOWN");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            var updatedAccount = await dbContext.ConnectedAccounts.FindAsync(account.Id);
            updatedAccount.Should().NotBeNull();
            updatedAccount!.Status.Should().Be(AccountStatus.COOLING_DOWN.ToDbString());
            updatedAccount.LastStatusUpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }
    }
}
