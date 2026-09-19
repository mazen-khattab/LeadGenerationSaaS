using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SaaS.Application.Common.Models;
using SaaS.Application.Features.Worker.Commands.UpdateJobStatus;
using SaaS.Application.UnitTests.Common;
using SaaS.Domain.Entities;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SaaS.Application.UnitTests.Features.Worker.Commands.UpdateJobStatus
{
    public class UpdateJobStatusCommandHandlerTests
    {
        private readonly Mock<ILogger<UpdateJobStatusCommandHandler>> _loggerMock;

        public UpdateJobStatusCommandHandlerTests()
        {
            _loggerMock = new Mock<ILogger<UpdateJobStatusCommandHandler>>();
        }

        private MockAppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<MockAppDbContext>()
                .UseInMemoryDatabase(databaseName: $"UpdateJobStatusDb_{Guid.NewGuid()}")
                .Options;

            return new MockAppDbContext(options);
        }

        [Fact]
        public void Constructor_WhenContextIsNull_ShouldThrowArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new UpdateJobStatusCommandHandler(null!, _loggerMock.Object));
            Assert.Equal("context", ex.ParamName);
        }

        [Fact]
        public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
        {
            using var dbContext = CreateDbContext();
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new UpdateJobStatusCommandHandler(dbContext, null!));
            Assert.Equal("logger", ex.ParamName);
        }

        [Fact]
        public async Task Handle_WhenJobNotFound_ShouldReturnNotFoundFailure()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var handler = new UpdateJobStatusCommandHandler(dbContext, _loggerMock.Object);
            var command = new UpdateJobStatusCommand(999, "Completed");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.NotFound, result.ErrorType);
            Assert.Equal("Job not found.", result.Message);
        }

        [Fact]
        public async Task Handle_WhenJobStatusUpdatedToProcessing_ShouldUpdateJobStatusOnly()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var job = new Job
            {
                Id = 1,
                UserId = Guid.NewGuid(),
                BotId = 1,
                Type = JobType.MESSAGING.ToDbString(),
                Status = JobStatus.PENDING.ToDbString(),
                PayloadJson = "{\"accountId\": 10}"
            };

            await dbContext.Jobs.AddAsync(job);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new UpdateJobStatusCommandHandler(dbContext, _loggerMock.Object);
            var command = new UpdateJobStatusCommand(job.Id, "Processing");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Data);
            Assert.Equal("Job status updated successfully.", result.Message);

            var updatedJob = await dbContext.Jobs.FindAsync(job.Id);
            Assert.NotNull(updatedJob);
            Assert.Equal(JobStatus.PROCESSING.ToDbString(), updatedJob.Status);
        }

        [Fact]
        public async Task Handle_WhenJobCompleted_ShouldSetAccountStatusToCoolingDown()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var accountId = 10;

            var account = new ConnectedAccount
            {
                Id = accountId,
                UserId = Guid.NewGuid(),
                BotId = 1,
                DisplayName = "Account 1",
                Status = AccountStatus.BUSY.ToDbString()
            };

            var job = new Job
            {
                Id = 5,
                UserId = account.UserId,
                BotId = 1,
                Type = JobType.MESSAGING.ToDbString(),
                Status = JobStatus.PROCESSING.ToDbString(),
                PayloadJson = $"{{\"accountId\": {accountId}}}"
            };

            await dbContext.ConnectedAccounts.AddAsync(account);
            await dbContext.Jobs.AddAsync(job);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new UpdateJobStatusCommandHandler(dbContext, _loggerMock.Object);
            var command = new UpdateJobStatusCommand(job.Id, "Completed");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);

            var updatedJob = await dbContext.Jobs.FindAsync(job.Id);
            Assert.NotNull(updatedJob);
            Assert.Equal(JobStatus.COMPLETED.ToDbString(), updatedJob.Status);

            var updatedAccount = await dbContext.ConnectedAccounts.FindAsync(accountId);
            Assert.NotNull(updatedAccount);
            Assert.Equal(AccountStatus.COOLING_DOWN.ToDbString(), updatedAccount.Status);
            Assert.NotEqual(default, updatedAccount.LastStatusUpdatedAt);
        }

        [Fact]
        public async Task Handle_WhenJobFailed_ShouldSetAccountStatusToActive()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var accountId = 15;

            var account = new ConnectedAccount
            {
                Id = accountId,
                UserId = Guid.NewGuid(),
                BotId = 1,
                DisplayName = "Account 2",
                Status = AccountStatus.BUSY.ToDbString()
            };

            var job = new Job
            {
                Id = 8,
                UserId = account.UserId,
                BotId = 1,
                Type = JobType.MESSAGING.ToDbString(),
                Status = JobStatus.PROCESSING.ToDbString(),
                PayloadJson = $"{{\"accountId\": {accountId}}}"
            };

            await dbContext.ConnectedAccounts.AddAsync(account);
            await dbContext.Jobs.AddAsync(job);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new UpdateJobStatusCommandHandler(dbContext, _loggerMock.Object);
            var command = new UpdateJobStatusCommand(job.Id, "Failed");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);

            var updatedJob = await dbContext.Jobs.FindAsync(job.Id);
            Assert.NotNull(updatedJob);
            Assert.Equal(JobStatus.FAILED.ToDbString(), updatedJob.Status);

            var updatedAccount = await dbContext.ConnectedAccounts.FindAsync(accountId);
            Assert.NotNull(updatedAccount);
            Assert.Equal(AccountStatus.ACTIVE.ToDbString(), updatedAccount.Status);
            Assert.NotEqual(default, updatedAccount.LastStatusUpdatedAt);
        }

        [Fact]
        public async Task Handle_WhenAccountStatusWasNotBusy_ShouldNotChangeAccountStatus()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var accountId = 20;

            var account = new ConnectedAccount
            {
                Id = accountId,
                UserId = Guid.NewGuid(),
                BotId = 1,
                DisplayName = "Account 3",
                Status = AccountStatus.COOLING_DOWN.ToDbString()
            };

            var job = new Job
            {
                Id = 12,
                UserId = account.UserId,
                BotId = 1,
                Type = JobType.MESSAGING.ToDbString(),
                Status = JobStatus.PROCESSING.ToDbString(),
                PayloadJson = $"{{\"accountId\": {accountId}}}"
            };

            await dbContext.ConnectedAccounts.AddAsync(account);
            await dbContext.Jobs.AddAsync(job);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new UpdateJobStatusCommandHandler(dbContext, _loggerMock.Object);
            var command = new UpdateJobStatusCommand(job.Id, "Completed");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);

            var updatedAccount = await dbContext.ConnectedAccounts.FindAsync(accountId);
            Assert.NotNull(updatedAccount);
            Assert.Equal(AccountStatus.COOLING_DOWN.ToDbString(), updatedAccount.Status);
        }
    }
}
