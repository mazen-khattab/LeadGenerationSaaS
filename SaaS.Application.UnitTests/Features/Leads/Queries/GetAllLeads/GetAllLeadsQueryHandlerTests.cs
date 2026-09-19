using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Application.Features.Leads.Queries.GetAllLeads;
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

namespace SaaS.Application.UnitTests.Features.Leads.Queries.GetAllLeads
{
    public class GetAllLeadsQueryHandlerTests
    {
        private readonly Mock<IUserBotService> _userBotServiceMock;
        private readonly Mock<ILogger<GetAllLeadsQueryHandler>> _loggerMock;

        public GetAllLeadsQueryHandlerTests()
        {
            _userBotServiceMock = new Mock<IUserBotService>();
            _loggerMock = new Mock<ILogger<GetAllLeadsQueryHandler>>();
        }

        private MockAppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<MockAppDbContext>()
                .UseInMemoryDatabase(databaseName: $"GetAllLeadsDb_{Guid.NewGuid()}")
                .Options;

            return new MockAppDbContext(options);
        }

        [Fact]
        public void Constructor_WhenContextIsNull_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new GetAllLeadsQueryHandler(null!, _userBotServiceMock.Object, _loggerMock.Object));

            Assert.Equal("context", ex.ParamName);
        }

        [Fact]
        public void Constructor_WhenUserBotServiceIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            using var dbContext = CreateDbContext();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new GetAllLeadsQueryHandler(dbContext, null!, _loggerMock.Object));

            Assert.Equal("userBotService", ex.ParamName);
        }

        [Fact]
        public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            using var dbContext = CreateDbContext();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new GetAllLeadsQueryHandler(dbContext, _userBotServiceMock.Object, null!));

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

            var handler = new GetAllLeadsQueryHandler(dbContext, _userBotServiceMock.Object, _loggerMock.Object);
            var query = new GetAllLeadsQuery(userId, botId, 1, 10, null);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.NotFound, result.ErrorType);
            Assert.Equal("User or bot not found", result.Message);
        }

        [Fact]
        public async Task Handle_WhenUserOwnsBotAndLeadsExist_ShouldReturnPaginatedLeadsSuccessfully()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var botId = 1;

            var group = new TargetGroup
            {
                Id = 10,
                UserId = userId,
                BotId = botId,
                GroupName = "Real Estate Agents"
            };

            var lead1 = new Lead
            {
                Id = 1,
                UserId = userId,
                BotId = botId,
                GroupId = group.Id,
                Group = group,
                ProfileName = "Alice Smith",
                ProfileUrl = "https://example.com/alice",
                AiMessage = "Hello Alice!",
                Status = LeadStatus.COMPLETED.ToDbString(),
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            };

            var lead2 = new Lead
            {
                Id = 2,
                UserId = userId,
                BotId = botId,
                GroupId = group.Id,
                Group = group,
                ProfileName = "Bob Jones",
                ProfileUrl = "https://example.com/bob",
                AiMessage = "Hello Bob!",
                Status = LeadStatus.PENDING.ToDbString(),
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            };

            var otherUserLead = new Lead
            {
                Id = 3,
                UserId = otherUserId,
                BotId = botId,
                ProfileName = "Other User Lead",
                ProfileUrl = "https://example.com/other",
                Status = LeadStatus.PENDING.ToDbString(),
                CreatedAt = DateTime.UtcNow
            };

            var otherBotLead = new Lead
            {
                Id = 4,
                UserId = userId,
                BotId = 999,
                ProfileName = "Other Bot Lead",
                ProfileUrl = "https://example.com/otherbot",
                Status = LeadStatus.PENDING.ToDbString(),
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.TargetGroups.AddAsync(group);
            await dbContext.Leads.AddRangeAsync(lead1, lead2, otherUserLead, otherBotLead);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new GetAllLeadsQueryHandler(dbContext, _userBotServiceMock.Object, _loggerMock.Object);
            var query = new GetAllLeadsQuery(userId, botId, 1, 10, null);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.TotalItems);
            Assert.Equal(1, result.Data.TotalPages);
            Assert.Equal(1, result.Data.CurrentPage);

            var items = result.Data.Items.ToList();
            Assert.Equal(2, items.Count);

            // Verify descending order by CreatedAt: lead2 is newer (-5 min) than lead1 (-10 min)
            Assert.Equal(2, items[0].LeadId);
            Assert.Equal("#LED-2", items[0].LeadFormattedId);
            Assert.Equal("Bob Jones", items[0].ProfileName);
            Assert.Equal("Real Estate Agents", items[0].FromGroup);

            Assert.Equal(1, items[1].LeadId);
            Assert.Equal("#LED-1", items[1].LeadFormattedId);
            Assert.Equal("Alice Smith", items[1].ProfileName);
            Assert.Equal("Real Estate Agents", items[1].FromGroup);
        }

        [Fact]
        public async Task Handle_WhenStatusFilterIsProvided_ShouldFilterByStatus()
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
                ProfileName = "Pending Lead",
                ProfileUrl = "https://example.com/1",
                Status = LeadStatus.PENDING.ToDbString(),
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            };

            var completedLead = new Lead
            {
                Id = 2,
                UserId = userId,
                BotId = botId,
                ProfileName = "Completed Lead",
                ProfileUrl = "https://example.com/2",
                Status = LeadStatus.COMPLETED.ToDbString(),
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.Leads.AddRangeAsync(pendingLead, completedLead);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new GetAllLeadsQueryHandler(dbContext, _userBotServiceMock.Object, _loggerMock.Object);
            var query = new GetAllLeadsQuery(userId, botId, 1, 10, LeadStatus.COMPLETED.ToDbString());

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(1, result.Data.TotalItems);

            var items = result.Data.Items.ToList();
            Assert.Single(items);
            Assert.Equal(2, items[0].LeadId);
            Assert.Equal(LeadStatus.COMPLETED.ToDbString(), items[0].Status);
        }

        [Fact]
        public async Task Handle_WhenPaginationIsApplied_ShouldSkipAndTakeCorrectPage()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;

            var leads = new List<Lead>();
            for (int i = 1; i <= 5; i++)
            {
                leads.Add(new Lead
                {
                    Id = i,
                    UserId = userId,
                    BotId = botId,
                    ProfileName = $"Lead {i}",
                    ProfileUrl = $"https://example.com/{i}",
                    Status = LeadStatus.PENDING.ToDbString(),
                    CreatedAt = DateTime.UtcNow.AddMinutes(i)
                });
            }

            await dbContext.Leads.AddRangeAsync(leads);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new GetAllLeadsQueryHandler(dbContext, _userBotServiceMock.Object, _loggerMock.Object);
            // Page 2 with PageSize 2
            var query = new GetAllLeadsQuery(userId, botId, 2, 2, null);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(5, result.Data.TotalItems);
            Assert.Equal(3, result.Data.TotalPages);
            Assert.Equal(2, result.Data.CurrentPage);

            var items = result.Data.Items.ToList();
            Assert.Equal(2, items.Count);

            // Ordered descending by CreatedAt: order of IDs is 5, 4, 3, 2, 1. Page 2 (skip 2, take 2) gives [3, 2]
            Assert.Equal(3, items[0].LeadId);
            Assert.Equal(2, items[1].LeadId);
        }

        [Fact]
        public async Task Handle_WhenNoLeadsExist_ShouldReturnEmptyPaginatedResult()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new GetAllLeadsQueryHandler(dbContext, _userBotServiceMock.Object, _loggerMock.Object);
            var query = new GetAllLeadsQuery(userId, botId, 1, 10, null);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(0, result.Data.TotalItems);
            Assert.Equal(0, result.Data.TotalPages);
            Assert.Equal(1, result.Data.CurrentPage);
            Assert.Empty(result.Data.Items);
        }

        [Fact]
        public async Task Handle_WhenLeadHasNoGroup_FromGroupShouldBeEmptyString()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;

            var lead = new Lead
            {
                Id = 1,
                UserId = userId,
                BotId = botId,
                GroupId = null,
                Group = null,
                ProfileName = "Solo Lead",
                ProfileUrl = "https://example.com/solo",
                Status = LeadStatus.PENDING.ToDbString(),
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.Leads.AddAsync(lead);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new GetAllLeadsQueryHandler(dbContext, _userBotServiceMock.Object, _loggerMock.Object);
            var query = new GetAllLeadsQuery(userId, botId, 1, 10, null);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);

            var items = result.Data.Items.ToList();
            Assert.Single(items);
            Assert.Equal(string.Empty, items[0].FromGroup);
        }

        [Fact]
        public async Task Handle_WhenLeadAiMessageIsNull_AiMessageShouldBeEmptyString()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;

            var lead = new Lead
            {
                Id = 1,
                UserId = userId,
                BotId = botId,
                ProfileName = "No Message Lead",
                ProfileUrl = "https://example.com/nomsg",
                AiMessage = null,
                Status = LeadStatus.PENDING.ToDbString(),
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.Leads.AddAsync(lead);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new GetAllLeadsQueryHandler(dbContext, _userBotServiceMock.Object, _loggerMock.Object);
            var query = new GetAllLeadsQuery(userId, botId, 1, 10, null);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);

            var items = result.Data.Items.ToList();
            Assert.Single(items);
            Assert.Equal(string.Empty, items[0].AiMessage);
        }
    }
}
