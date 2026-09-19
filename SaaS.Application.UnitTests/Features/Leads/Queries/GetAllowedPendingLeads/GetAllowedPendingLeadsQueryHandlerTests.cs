using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Application.Common.Settings;
using SaaS.Application.Features.Leads.Queries.GetAllowedPendingLeads;
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

namespace SaaS.Application.UnitTests.Features.Leads.Queries.GetAllowedPendingLeads
{
    public class GetAllowedPendingLeadsQueryHandlerTests
    {
        private readonly Mock<IUserBotService> _userBotServiceMock;
        private readonly Mock<IOptionsSnapshot<GeneralSettings>> _optionsMock;
        private readonly Mock<ILogger<GetAllowedPendingLeadsQueryHandler>> _loggerMock;
        private readonly GeneralSettings _generalSettings;

        public GetAllowedPendingLeadsQueryHandlerTests()
        {
            _userBotServiceMock = new Mock<IUserBotService>();
            _optionsMock = new Mock<IOptionsSnapshot<GeneralSettings>>();
            _loggerMock = new Mock<ILogger<GetAllowedPendingLeadsQueryHandler>>();

            _generalSettings = new GeneralSettings
            {
                DailyLimitResetHour = 12,
                AccountCooldownperiodDays = 1
            };

            _optionsMock.Setup(o => o.Value).Returns(_generalSettings);
        }

        private MockAppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<MockAppDbContext>()
                .UseInMemoryDatabase(databaseName: $"GetAllowedPendingLeadsDb_{Guid.NewGuid()}")
                .Options;

            return new MockAppDbContext(options);
        }

        [Fact]
        public void Constructor_WhenContextIsNull_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new GetAllowedPendingLeadsQueryHandler(null!, _userBotServiceMock.Object, _optionsMock.Object, _loggerMock.Object));

            Assert.Equal("context", ex.ParamName);
        }

        [Fact]
        public void Constructor_WhenUserBotServiceIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            using var dbContext = CreateDbContext();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new GetAllowedPendingLeadsQueryHandler(dbContext, null!, _optionsMock.Object, _loggerMock.Object));

            Assert.Equal("userBotService", ex.ParamName);
        }

        [Fact]
        public void Constructor_WhenOptionsIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            using var dbContext = CreateDbContext();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new GetAllowedPendingLeadsQueryHandler(dbContext, _userBotServiceMock.Object, null!, _loggerMock.Object));

            Assert.Equal("options", ex.ParamName);
        }

        [Fact]
        public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            using var dbContext = CreateDbContext();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new GetAllowedPendingLeadsQueryHandler(dbContext, _userBotServiceMock.Object, _optionsMock.Object, null!));

            Assert.Equal("logger", ex.ParamName);
        }

        [Fact]
        public async Task Handle_WhenUserDoesNotOwnBot_ShouldReturnNotFoundFailure()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var handler = new GetAllowedPendingLeadsQueryHandler(
                dbContext, _userBotServiceMock.Object, _optionsMock.Object, _loggerMock.Object);
            var query = new GetAllowedPendingLeadsQuery(userId, botId);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.NotFound, result.ErrorType);
            Assert.Equal("User or bot not found", result.Message);
        }

        [Fact]
        public async Task Handle_WhenUserSettingsExist_ShouldUseConfiguredDailyMessageLimit()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;

            var userSetting = new UserSetting
            {
                Id = 1,
                UserId = userId,
                DailyMessageLimit = 25
            };

            await dbContext.UserSettings.AddAsync(userSetting);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new GetAllowedPendingLeadsQueryHandler(
                dbContext, _userBotServiceMock.Object, _optionsMock.Object, _loggerMock.Object);
            var query = new GetAllowedPendingLeadsQuery(userId, botId);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(25, result.Data.DailyMessageLimit);
            Assert.Equal(0, result.Data.MessagesSent);
            Assert.Equal(25, result.Data.AllowedToProcessCount);
            Assert.Empty(result.Data.Leads);
        }

        [Fact]
        public async Task Handle_WhenUserSettingsDoNotExist_ShouldFallbackToDefault50DailyLimit()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new GetAllowedPendingLeadsQueryHandler(
                dbContext, _userBotServiceMock.Object, _optionsMock.Object, _loggerMock.Object);
            var query = new GetAllowedPendingLeadsQuery(userId, botId);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(50, result.Data.DailyMessageLimit);
            Assert.Equal(0, result.Data.MessagesSent);
            Assert.Equal(50, result.Data.AllowedToProcessCount);
        }

        [Fact]
        public async Task Handle_WhenMessagesSentWithinWindow_ShouldDeductFromRemainingCount()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;

            var userSetting = new UserSetting
            {
                Id = 1,
                UserId = userId,
                DailyMessageLimit = 10
            };

            // Completed within 12h window -> counted
            var sent1 = new Lead
            {
                Id = 1,
                UserId = userId,
                BotId = botId,
                Status = LeadStatus.COMPLETED.ToDbString(),
                ProcessedAt = DateTime.UtcNow.AddHours(-2)
            };
            var sent2 = new Lead
            {
                Id = 2,
                UserId = userId,
                BotId = botId,
                Status = LeadStatus.COMPLETED.ToDbString(),
                ProcessedAt = DateTime.UtcNow.AddHours(-4)
            };

            // Completed before 12h window -> NOT counted
            var oldSent = new Lead
            {
                Id = 3,
                UserId = userId,
                BotId = botId,
                Status = LeadStatus.COMPLETED.ToDbString(),
                ProcessedAt = DateTime.UtcNow.AddHours(-20)
            };

            // Failed within window -> NOT counted
            var failed = new Lead
            {
                Id = 4,
                UserId = userId,
                BotId = botId,
                Status = LeadStatus.FAILED.ToDbString(),
                ProcessedAt = DateTime.UtcNow.AddHours(-1)
            };

            // Completed within window but for DIFFERENT bot -> NOT counted
            var diffBot = new Lead
            {
                Id = 5,
                UserId = userId,
                BotId = 999,
                Status = LeadStatus.COMPLETED.ToDbString(),
                ProcessedAt = DateTime.UtcNow.AddHours(-1)
            };

            // Pending lead
            var pendingLead = new Lead
            {
                Id = 6,
                UserId = userId,
                BotId = botId,
                Status = LeadStatus.PENDING.ToDbString(),
                ProfileName = "Pending 1",
                ProfileUrl = "https://example.com/p1",
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.UserSettings.AddAsync(userSetting);
            await dbContext.Leads.AddRangeAsync(sent1, sent2, oldSent, failed, diffBot, pendingLead);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new GetAllowedPendingLeadsQueryHandler(
                dbContext, _userBotServiceMock.Object, _optionsMock.Object, _loggerMock.Object);
            var query = new GetAllowedPendingLeadsQuery(userId, botId);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(10, result.Data.DailyMessageLimit);
            Assert.Equal(2, result.Data.MessagesSent);
            Assert.Equal(8, result.Data.AllowedToProcessCount);
            Assert.Single(result.Data.Leads);
            Assert.Equal(6, result.Data.Leads[0].LeadId);
        }

        [Fact]
        public async Task Handle_WhenMessagesSentExceedsOrEqualsDailyLimit_ShouldReturnZeroRemainingAndEmptyLeads()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;

            var userSetting = new UserSetting
            {
                Id = 1,
                UserId = userId,
                DailyMessageLimit = 2
            };

            var sentLeads = new List<Lead>
            {
                new Lead { Id = 1, UserId = userId, BotId = botId, Status = LeadStatus.COMPLETED.ToDbString(), ProcessedAt = DateTime.UtcNow.AddHours(-1) },
                new Lead { Id = 2, UserId = userId, BotId = botId, Status = LeadStatus.COMPLETED.ToDbString(), ProcessedAt = DateTime.UtcNow.AddHours(-1) },
                new Lead { Id = 3, UserId = userId, BotId = botId, Status = LeadStatus.COMPLETED.ToDbString(), ProcessedAt = DateTime.UtcNow.AddHours(-1) }
            };

            var pendingLead = new Lead
            {
                Id = 4,
                UserId = userId,
                BotId = botId,
                Status = LeadStatus.PENDING.ToDbString(),
                ProfileName = "Pending Lead",
                ProfileUrl = "https://example.com/p",
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.UserSettings.AddAsync(userSetting);
            await dbContext.Leads.AddRangeAsync(sentLeads);
            await dbContext.Leads.AddAsync(pendingLead);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new GetAllowedPendingLeadsQueryHandler(
                dbContext, _userBotServiceMock.Object, _optionsMock.Object, _loggerMock.Object);
            var query = new GetAllowedPendingLeadsQuery(userId, botId);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.DailyMessageLimit);
            Assert.Equal(3, result.Data.MessagesSent);
            Assert.Equal(0, result.Data.AllowedToProcessCount);
            Assert.Empty(result.Data.Leads);
        }

        [Fact]
        public async Task Handle_WhenRemainingIsPositive_ShouldTakeUpToRemainingPendingLeadsOrderedByCreatedAt()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;

            var userSetting = new UserSetting
            {
                Id = 1,
                UserId = userId,
                DailyMessageLimit = 3
            };

            var pendingLeads = new List<Lead>();
            for (int i = 1; i <= 5; i++)
            {
                pendingLeads.Add(new Lead
                {
                    Id = i,
                    UserId = userId,
                    BotId = botId,
                    Status = LeadStatus.PENDING.ToDbString(),
                    ProfileName = $"Lead {i}",
                    ProfileUrl = $"https://example.com/{i}",
                    AiMessage = $"Message {i}",
                    CreatedAt = DateTime.UtcNow.AddMinutes(i)
                });
            }

            // Also add pending lead for another user and another bot to ensure isolation
            var otherUserLead = new Lead
            {
                Id = 10,
                UserId = Guid.NewGuid(),
                BotId = botId,
                Status = LeadStatus.PENDING.ToDbString(),
                CreatedAt = DateTime.UtcNow
            };

            var otherBotLead = new Lead
            {
                Id = 11,
                UserId = userId,
                BotId = 888,
                Status = LeadStatus.PENDING.ToDbString(),
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.UserSettings.AddAsync(userSetting);
            await dbContext.Leads.AddRangeAsync(pendingLeads);
            await dbContext.Leads.AddRangeAsync(otherUserLead, otherBotLead);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new GetAllowedPendingLeadsQueryHandler(
                dbContext, _userBotServiceMock.Object, _optionsMock.Object, _loggerMock.Object);
            var query = new GetAllowedPendingLeadsQuery(userId, botId);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(3, result.Data.DailyMessageLimit);
            Assert.Equal(0, result.Data.MessagesSent);
            Assert.Equal(3, result.Data.AllowedToProcessCount);
            Assert.Equal(3, result.Data.Leads.Count);

            // Ordered ascending by CreatedAt (earliest first): 1, 2, 3
            Assert.Equal(1, result.Data.Leads[0].LeadId);
            Assert.Equal("#LED-1", result.Data.Leads[0].LeadNumber);
            Assert.Equal("Lead 1", result.Data.Leads[0].ProfileName);
            Assert.Equal("Message 1", result.Data.Leads[0].AiMessage);

            Assert.Equal(2, result.Data.Leads[1].LeadId);
            Assert.Equal("#LED-2", result.Data.Leads[1].LeadNumber);

            Assert.Equal(3, result.Data.Leads[2].LeadId);
            Assert.Equal("#LED-3", result.Data.Leads[2].LeadNumber);
        }

        [Fact]
        public async Task Handle_WhenLeadAiMessageIsNull_ShouldMapToEmptyString()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;

            var pendingLead = new Lead
            {
                Id = 1,
                UserId = userId,
                BotId = botId,
                Status = LeadStatus.PENDING.ToDbString(),
                ProfileName = "No AI Message",
                ProfileUrl = "https://example.com/no-ai",
                AiMessage = null,
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.Leads.AddAsync(pendingLead);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new GetAllowedPendingLeadsQueryHandler(
                dbContext, _userBotServiceMock.Object, _optionsMock.Object, _loggerMock.Object);
            var query = new GetAllowedPendingLeadsQuery(userId, botId);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Single(result.Data.Leads);
            Assert.Equal(string.Empty, result.Data.Leads[0].AiMessage);
        }
    }
}
