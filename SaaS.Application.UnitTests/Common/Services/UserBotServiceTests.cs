using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SaaS.Application.Common.Services;
using SaaS.Application.UnitTests.Common;
using SaaS.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SaaS.Application.UnitTests.Common.Services
{
    public class UserBotServiceTests
    {
        private MockAppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<MockAppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new MockAppDbContext(options);
        }

        [Fact]
        public async Task CheckOwnershipAsync_WhenOwnershipExists_ReturnsTrue()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;

            await dbContext.UserBots.AddAsync(new UserBot
            {
                UserId = userId,
                BotId = botId
            });
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var service = new UserBotService(dbContext);

            // Act
            var result = await service.CheckOwnershipAsync(userId, botId, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CheckOwnershipAsync_WhenUserIdMatchesButBotIdDiffers_ReturnsFalse()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var userId = Guid.NewGuid();
            var botIdInDb = 1;
            var requestedBotId = 2;

            await dbContext.UserBots.AddAsync(new UserBot
            {
                UserId = userId,
                BotId = botIdInDb
            });
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var service = new UserBotService(dbContext);

            // Act
            var result = await service.CheckOwnershipAsync(userId, requestedBotId, CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task CheckOwnershipAsync_WhenBotIdMatchesButUserIdDiffers_ReturnsFalse()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var ownerUserId = Guid.NewGuid();
            var nonOwnerUserId = Guid.NewGuid();
            var botId = 1;

            await dbContext.UserBots.AddAsync(new UserBot
            {
                UserId = ownerUserId,
                BotId = botId
            });
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var service = new UserBotService(dbContext);

            // Act
            var result = await service.CheckOwnershipAsync(nonOwnerUserId, botId, CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task CheckOwnershipAsync_WhenDatabaseIsEmpty_ReturnsFalse()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var service = new UserBotService(dbContext);

            // Act
            var result = await service.CheckOwnershipAsync(Guid.NewGuid(), 1, CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task CheckOwnershipAsync_WhenMultipleUserBotsExist_ReturnsCorrectResult()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            await dbContext.UserBots.AddRangeAsync(
                new UserBot { UserId = user1, BotId = 1 },
                new UserBot { UserId = user1, BotId = 2 },
                new UserBot { UserId = user2, BotId = 3 }
            );
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var service = new UserBotService(dbContext);

            // Act & Assert
            (await service.CheckOwnershipAsync(user1, 1, CancellationToken.None)).Should().BeTrue();
            (await service.CheckOwnershipAsync(user1, 2, CancellationToken.None)).Should().BeTrue();
            (await service.CheckOwnershipAsync(user1, 3, CancellationToken.None)).Should().BeFalse();
            (await service.CheckOwnershipAsync(user2, 3, CancellationToken.None)).Should().BeTrue();
            (await service.CheckOwnershipAsync(user2, 1, CancellationToken.None)).Should().BeFalse();
        }
    }
}
