using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Application.Features.Runs.Commands.Complete;
using SaaS.Application.UnitTests.Common;
using SaaS.Domain.Entities;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SaaS.Application.UnitTests.Features.Runs.Commands.Complete
{
    public class CompleteRunCommandHandlerTests
    {
        private readonly Mock<IAppNotificationService> _notificationServiceMock;
        private readonly Mock<ILogger<CompleteRunCommandHandler>> _loggerMock;

        public CompleteRunCommandHandlerTests()
        {
            _notificationServiceMock = new Mock<IAppNotificationService>();
            _loggerMock = new Mock<ILogger<CompleteRunCommandHandler>>();
        }

        private MockAppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<MockAppDbContext>()
                .UseInMemoryDatabase(databaseName: $"CompleteRunDb_{Guid.NewGuid()}")
                .Options;

            return new MockAppDbContext(options);
        }

        [Fact]
        public void Constructor_WhenContextIsNull_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new CompleteRunCommandHandler(null!, _notificationServiceMock.Object, _loggerMock.Object));

            Assert.Equal("context", ex.ParamName);
        }

        [Fact]
        public void Constructor_WhenNotificationServiceIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            using var dbContext = CreateDbContext();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new CompleteRunCommandHandler(dbContext, null!, _loggerMock.Object));

            Assert.Equal("notificationService", ex.ParamName);
        }

        [Fact]
        public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            using var dbContext = CreateDbContext();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new CompleteRunCommandHandler(dbContext, _notificationServiceMock.Object, null!));

            Assert.Equal("logger", ex.ParamName);
        }

        [Fact]
        public async Task Handle_WhenRunNotFound_ShouldReturnNotFoundFailure()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var handler = new CompleteRunCommandHandler(dbContext, _notificationServiceMock.Object, _loggerMock.Object);
            var command = new CompleteRunCommand(999, new List<ScrapedLeadDto>());

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.NotFound, result.ErrorType);
            Assert.Equal("Run not found.", result.Message);
        }

        [Theory]
        [InlineData("Completed")]
        [InlineData("Failed")]
        [InlineData("Pending")]
        public async Task Handle_WhenRunIsNotRunning_ShouldReturnValidationError(string invalidStatus)
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var run = new Run
            {
                Id = 1,
                UserId = Guid.NewGuid(),
                BotId = 1,
                Status = invalidStatus,
                StartedAt = DateTime.UtcNow.AddMinutes(-10)
            };

            await dbContext.Runs.AddAsync(run);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new CompleteRunCommandHandler(dbContext, _notificationServiceMock.Object, _loggerMock.Object);
            var command = new CompleteRunCommand(run.Id, new List<ScrapedLeadDto>());

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.ValidationError, result.ErrorType);
            Assert.Equal("Run is not in progress or has already been finalized.", result.Message);
        }

        [Fact]
        public async Task Handle_WhenNoIncomingLeads_ShouldCompleteRunWithZeroCollectedLeads()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var accountId = 10;

            var account = new ConnectedAccount
            {
                Id = accountId,
                UserId = userId,
                DisplayName = "Test Account",
                Status = AccountStatus.BUSY.ToDbString()
            };

            var run = new Run
            {
                Id = 1,
                UserId = userId,
                BotId = 1,
                AccountId = accountId,
                Status = RunStatus.RUNNING.ToDbString(),
                StartedAt = DateTime.UtcNow.AddMinutes(-5)
            };

            await dbContext.ConnectedAccounts.AddAsync(account);
            await dbContext.Runs.AddAsync(run);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new CompleteRunCommandHandler(dbContext, _notificationServiceMock.Object, _loggerMock.Object);
            var command = new CompleteRunCommand(run.Id, new List<ScrapedLeadDto>());

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Data);
            Assert.Equal("Run completed with no leads.", result.Message);

            var updatedRun = await dbContext.Runs.FindAsync(run.Id);
            Assert.NotNull(updatedRun);
            Assert.Equal(RunStatus.COMPLETED.ToDbString(), updatedRun.Status);
            Assert.Equal(0, updatedRun.CollectedLeadsCount);
            Assert.NotNull(updatedRun.EndedAt);

            var updatedAccount = await dbContext.ConnectedAccounts.FindAsync(accountId);
            Assert.NotNull(updatedAccount);
            Assert.Equal(AccountStatus.ACTIVE.ToDbString(), updatedAccount.Status);
            Assert.NotEqual(default, updatedAccount.LastStatusUpdatedAt);

            _notificationServiceMock.Verify(n =>
                n.NotifyRunCompletedAsync(userId, run.Id, 0), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenIncomingLeadsAreUnique_ShouldPersistAllLeadsAndNotify()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var accountId = 5;
            var groupId = 20;

            var account = new ConnectedAccount
            {
                Id = accountId,
                UserId = userId,
                DisplayName = "Active Account",
                Status = AccountStatus.BUSY.ToDbString()
            };

            var run = new Run
            {
                Id = 10,
                UserId = userId,
                BotId = 2,
                AccountId = accountId,
                GroupId = groupId,
                Status = RunStatus.RUNNING.ToDbString(),
                StartedAt = DateTime.UtcNow.AddMinutes(-15)
            };

            await dbContext.ConnectedAccounts.AddAsync(account);
            await dbContext.Runs.AddAsync(run);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var incomingLeads = new List<ScrapedLeadDto>
            {
                new ScrapedLeadDto("ext-101", "User1", "Full Name 1", "Ai message 1", "{\"city\":\"Cairo\"}"),
                new ScrapedLeadDto("ext-102", "User2", "Full Name 2", "Ai message 2", "{\"city\":\"Alexandria\"}")
            };

            var handler = new CompleteRunCommandHandler(dbContext, _notificationServiceMock.Object, _loggerMock.Object);
            var command = new CompleteRunCommand(run.Id, incomingLeads);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Data);
            Assert.Equal("Run completed and unique leads persisted.", result.Message);

            var updatedRun = await dbContext.Runs.FindAsync(run.Id);
            Assert.NotNull(updatedRun);
            Assert.Equal(RunStatus.COMPLETED.ToDbString(), updatedRun.Status);
            Assert.Equal(2, updatedRun.CollectedLeadsCount);
            Assert.NotNull(updatedRun.EndedAt);

            var persistedLeads = await dbContext.Leads.Include(l => l.Detail).Where(l => l.RunId == run.Id).ToListAsync();
            Assert.Equal(2, persistedLeads.Count);

            var lead1 = persistedLeads.FirstOrDefault(l => l.ExternalId == "ext-101");
            Assert.NotNull(lead1);
            Assert.Equal(userId, lead1.UserId);
            Assert.Equal(2, lead1.BotId);
            Assert.Equal(groupId, lead1.GroupId);
            Assert.Equal(accountId, lead1.AccountId);
            Assert.Equal("User1", lead1.ProfileName);
            Assert.Equal("ext-101", lead1.ProfileUrl);
            Assert.Equal("Ai message 1", lead1.AiMessage);
            Assert.Equal(LeadStatus.PENDING.ToDbString(), lead1.Status);
            Assert.NotNull(lead1.Detail);
            Assert.Equal("{\"city\":\"Cairo\"}", lead1.Detail.MetaDataJson);

            _notificationServiceMock.Verify(n =>
                n.NotifyRunCompletedAsync(userId, run.Id, 2), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenIncomingLeadsContainDuplicatesFromDb_ShouldOnlyPersistUniqueLeads()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();

            var run = new Run
            {
                Id = 1,
                UserId = userId,
                BotId = 1,
                Status = RunStatus.RUNNING.ToDbString(),
                StartedAt = DateTime.UtcNow.AddMinutes(-5)
            };

            // Existing lead for the same user with ExternalId "ext-existing"
            var existingLead = new Lead
            {
                Id = 100,
                UserId = userId,
                BotId = 1,
                ExternalId = "ext-existing",
                ProfileName = "Already In DB",
                Status = LeadStatus.COMPLETED.ToDbString(),
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            await dbContext.Runs.AddAsync(run);
            await dbContext.Leads.AddAsync(existingLead);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var incomingLeads = new List<ScrapedLeadDto>
            {
                new ScrapedLeadDto("ext-existing", "Duplicate User", "Dup Full", "Msg", "{}"),
                new ScrapedLeadDto("ext-brand-new", "Brand New User", "New Full", "Msg 2", "{}")
            };

            var handler = new CompleteRunCommandHandler(dbContext, _notificationServiceMock.Object, _loggerMock.Object);
            var command = new CompleteRunCommand(run.Id, incomingLeads);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Data);

            var updatedRun = await dbContext.Runs.FindAsync(run.Id);
            Assert.NotNull(updatedRun);
            Assert.Equal(1, updatedRun.CollectedLeadsCount);

            var allLeads = await dbContext.Leads.Where(l => l.UserId == userId).ToListAsync();
            Assert.Equal(2, allLeads.Count);
            Assert.Contains(allLeads, l => l.ExternalId == "ext-existing" && l.Id == 100);
            Assert.Contains(allLeads, l => l.ExternalId == "ext-brand-new" && l.RunId == run.Id);

            _notificationServiceMock.Verify(n =>
                n.NotifyRunCompletedAsync(userId, run.Id, 1), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenAllIncomingLeadsAreDuplicates_ShouldPersistZeroLeads()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();

            var run = new Run
            {
                Id = 1,
                UserId = userId,
                BotId = 1,
                Status = RunStatus.RUNNING.ToDbString(),
                StartedAt = DateTime.UtcNow.AddMinutes(-5)
            };

            var existingLead = new Lead
            {
                Id = 100,
                UserId = userId,
                BotId = 1,
                ExternalId = "ext-dup",
                Status = LeadStatus.COMPLETED.ToDbString()
            };

            await dbContext.Runs.AddAsync(run);
            await dbContext.Leads.AddAsync(existingLead);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var incomingLeads = new List<ScrapedLeadDto>
            {
                new ScrapedLeadDto("ext-dup", "Duplicate", "Dup Full", null!, null!)
            };

            var handler = new CompleteRunCommandHandler(dbContext, _notificationServiceMock.Object, _loggerMock.Object);
            var command = new CompleteRunCommand(run.Id, incomingLeads);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Run completed with no leads.", result.Message);

            var updatedRun = await dbContext.Runs.FindAsync(run.Id);
            Assert.NotNull(updatedRun);
            Assert.Equal(0, updatedRun.CollectedLeadsCount);

            _notificationServiceMock.Verify(n =>
                n.NotifyRunCompletedAsync(userId, run.Id, 0), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenIncomingLeadsHaveWhitespaceExternalIds_ShouldIgnoreThem()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();

            var run = new Run
            {
                Id = 1,
                UserId = userId,
                BotId = 1,
                Status = RunStatus.RUNNING.ToDbString(),
                StartedAt = DateTime.UtcNow.AddMinutes(-5)
            };

            await dbContext.Runs.AddAsync(run);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var incomingLeads = new List<ScrapedLeadDto>
            {
                new ScrapedLeadDto("", "Empty Id", "Empty Full", null!, null!),
                new ScrapedLeadDto("   ", "Whitespace Id", "White Full", null!, null!)
            };

            var handler = new CompleteRunCommandHandler(dbContext, _notificationServiceMock.Object, _loggerMock.Object);
            var command = new CompleteRunCommand(run.Id, incomingLeads);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Run completed with no leads.", result.Message);

            var updatedRun = await dbContext.Runs.FindAsync(run.Id);
            Assert.NotNull(updatedRun);
            Assert.Equal(0, updatedRun.CollectedLeadsCount);
        }

        [Fact]
        public async Task Handle_WhenNotificationThrowsException_ShouldStillSucceedAndLog()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();

            var run = new Run
            {
                Id = 1,
                UserId = userId,
                BotId = 1,
                Status = RunStatus.RUNNING.ToDbString(),
                StartedAt = DateTime.UtcNow.AddMinutes(-5)
            };

            await dbContext.Runs.AddAsync(run);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _notificationServiceMock
                .Setup(n => n.NotifyRunCompletedAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("SignalR connection failed"));

            var handler = new CompleteRunCommandHandler(dbContext, _notificationServiceMock.Object, _loggerMock.Object);
            var command = new CompleteRunCommand(run.Id, new List<ScrapedLeadDto>());

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert - handler catches notification exception and still returns success
            Assert.True(result.IsSuccess);
            Assert.True(result.Data);

            var updatedRun = await dbContext.Runs.FindAsync(run.Id);
            Assert.NotNull(updatedRun);
            Assert.Equal(RunStatus.COMPLETED.ToDbString(), updatedRun.Status);
        }

        [Fact]
        public async Task Handle_WhenAccountStatusIsNotBusy_ShouldNotChangeAccountStatus()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var accountId = 12;

            var account = new ConnectedAccount
            {
                Id = accountId,
                UserId = userId,
                DisplayName = "Cooldown Account",
                Status = AccountStatus.COOLING_DOWN.ToDbString()
            };

            var run = new Run
            {
                Id = 1,
                UserId = userId,
                BotId = 1,
                AccountId = accountId,
                Status = RunStatus.RUNNING.ToDbString(),
                StartedAt = DateTime.UtcNow.AddMinutes(-5)
            };

            await dbContext.ConnectedAccounts.AddAsync(account);
            await dbContext.Runs.AddAsync(run);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new CompleteRunCommandHandler(dbContext, _notificationServiceMock.Object, _loggerMock.Object);
            var command = new CompleteRunCommand(run.Id, new List<ScrapedLeadDto>());

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);

            var updatedAccount = await dbContext.ConnectedAccounts.FindAsync(accountId);
            Assert.NotNull(updatedAccount);
            // Status remains COOLING_DOWN because it was not BUSY
            Assert.Equal(AccountStatus.COOLING_DOWN.ToDbString(), updatedAccount.Status);
        }
    }
}
