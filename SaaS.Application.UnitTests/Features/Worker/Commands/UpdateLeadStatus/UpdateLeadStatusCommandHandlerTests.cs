using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Application.Features.Worker.Commands.UpdateLeadStatus;
using SaaS.Application.UnitTests.Common;
using SaaS.Domain.Entities;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SaaS.Application.UnitTests.Features.Worker.Commands.UpdateLeadStatus
{
    public class UpdateLeadStatusCommandHandlerTests
    {
        private readonly Mock<IAppNotificationService> _notificationServiceMock;
        private readonly Mock<ILogger<UpdateLeadStatusCommandHandler>> _loggerMock;

        public UpdateLeadStatusCommandHandlerTests()
        {
            _notificationServiceMock = new Mock<IAppNotificationService>();
            _loggerMock = new Mock<ILogger<UpdateLeadStatusCommandHandler>>();
        }

        private MockAppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<MockAppDbContext>()
                .UseInMemoryDatabase(databaseName: $"UpdateLeadStatusDb_{Guid.NewGuid()}")
                .Options;

            return new MockAppDbContext(options);
        }

        [Fact]
        public void Constructor_WhenContextIsNull_ShouldThrowArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new UpdateLeadStatusCommandHandler(null!, _notificationServiceMock.Object, _loggerMock.Object));
            Assert.Equal("context", ex.ParamName);
        }

        [Fact]
        public void Constructor_WhenNotificationServiceIsNull_ShouldThrowArgumentNullException()
        {
            using var dbContext = CreateDbContext();
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new UpdateLeadStatusCommandHandler(dbContext, null!, _loggerMock.Object));
            Assert.Equal("notificationService", ex.ParamName);
        }

        [Fact]
        public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
        {
            using var dbContext = CreateDbContext();
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new UpdateLeadStatusCommandHandler(dbContext, _notificationServiceMock.Object, null!));
            Assert.Equal("logger", ex.ParamName);
        }

        [Fact]
        public async Task Handle_WhenLeadNotFound_ShouldReturnNotFoundFailure()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var handler = new UpdateLeadStatusCommandHandler(dbContext, _notificationServiceMock.Object, _loggerMock.Object);
            var command = new UpdateLeadStatusCommand(999, "Completed");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.NotFound, result.ErrorType);
            Assert.Equal("Lead not found.", result.Message);
        }

        [Fact]
        public async Task Handle_WhenStatusUpdatedToCompleted_ShouldSetProcessedAtAndNotify()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var lead = new Lead
            {
                Id = 1,
                UserId = userId,
                BotId = 1,
                ProfileName = "Alice",
                Status = LeadStatus.PENDING.ToDbString(),
                ProcessedAt = null
            };

            await dbContext.Leads.AddAsync(lead);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new UpdateLeadStatusCommandHandler(dbContext, _notificationServiceMock.Object, _loggerMock.Object);
            var command = new UpdateLeadStatusCommand(lead.Id, "Completed");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Data);
            Assert.Equal("Lead status updated successfully.", result.Message);

            var updatedLead = await dbContext.Leads.FindAsync(lead.Id);
            Assert.NotNull(updatedLead);
            Assert.Equal(LeadStatus.COMPLETED.ToDbString(), updatedLead.Status);
            Assert.NotNull(updatedLead.ProcessedAt);

            _notificationServiceMock.Verify(n =>
                n.NotifyLeadStatusUpdatedAsync(userId, lead.Id, LeadStatus.COMPLETED.ToDbString()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenStatusUpdatedToFailed_ShouldSetProcessedAtAndNotify()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var lead = new Lead
            {
                Id = 2,
                UserId = userId,
                BotId = 1,
                ProfileName = "Bob",
                Status = LeadStatus.PENDING.ToDbString(),
                ProcessedAt = null
            };

            await dbContext.Leads.AddAsync(lead);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new UpdateLeadStatusCommandHandler(dbContext, _notificationServiceMock.Object, _loggerMock.Object);
            var command = new UpdateLeadStatusCommand(lead.Id, "Failed");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);

            var updatedLead = await dbContext.Leads.FindAsync(lead.Id);
            Assert.NotNull(updatedLead);
            Assert.Equal(LeadStatus.FAILED.ToDbString(), updatedLead.Status);
            Assert.NotNull(updatedLead.ProcessedAt);

            _notificationServiceMock.Verify(n =>
                n.NotifyLeadStatusUpdatedAsync(userId, lead.Id, LeadStatus.FAILED.ToDbString()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenStatusUpdatedToPending_ShouldNotSetProcessedAt()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var lead = new Lead
            {
                Id = 3,
                UserId = userId,
                BotId = 1,
                ProfileName = "Charlie",
                Status = LeadStatus.PENDING.ToDbString(),
                ProcessedAt = null
            };

            await dbContext.Leads.AddAsync(lead);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new UpdateLeadStatusCommandHandler(dbContext, _notificationServiceMock.Object, _loggerMock.Object);
            var command = new UpdateLeadStatusCommand(lead.Id, "Pending");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);

            var updatedLead = await dbContext.Leads.FindAsync(lead.Id);
            Assert.NotNull(updatedLead);
            Assert.Equal(LeadStatus.PENDING.ToDbString(), updatedLead.Status);
            Assert.Null(updatedLead.ProcessedAt);

            _notificationServiceMock.Verify(n =>
                n.NotifyLeadStatusUpdatedAsync(userId, lead.Id, LeadStatus.PENDING.ToDbString()), Times.Once);
        }
    }
}
