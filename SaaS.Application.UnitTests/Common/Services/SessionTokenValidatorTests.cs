using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SaaS.Application.Common.Services;
using SaaS.Application.UnitTests.Common;
using SaaS.Domain.Entities;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SaaS.Application.UnitTests.Common.Services
{
    public class SessionTokenValidatorTests
    {
        private MockAppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<MockAppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new MockAppDbContext(options);
        }

        [Fact]
        public async Task GetCurrentSessionTokenAsync_WhenUserExistsAndHasToken_ReturnsSessionToken()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var expectedToken = "session-token-123";
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                FullName = "Test User",
                PasswordHash = "hash",
                CurrentSessionToken = expectedToken
            };

            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync(default);

            var validator = new SessionTokenValidator(dbContext);

            // Act
            var result = await validator.GetCurrentSessionTokenAsync(user.Id);

            // Assert
            result.Should().Be(expectedToken);
        }

        [Fact]
        public async Task GetCurrentSessionTokenAsync_WhenUserExistsAndHasNullToken_ReturnsNull()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                FullName = "Test User",
                PasswordHash = "hash",
                CurrentSessionToken = null
            };

            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync(default);

            var validator = new SessionTokenValidator(dbContext);

            // Act
            var result = await validator.GetCurrentSessionTokenAsync(user.Id);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetCurrentSessionTokenAsync_WhenUserDoesNotExist_ReturnsNull()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var nonExistentUserId = Guid.NewGuid();

            var validator = new SessionTokenValidator(dbContext);

            // Act
            var result = await validator.GetCurrentSessionTokenAsync(nonExistentUserId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetCurrentSessionTokenAsync_WhenMultipleUsersExist_ReturnsCorrectUserToken()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var user1 = new User
            {
                Id = Guid.NewGuid(),
                Email = "user1@example.com",
                FullName = "User One",
                PasswordHash = "hash",
                CurrentSessionToken = "token-user-1"
            };
            var user2 = new User
            {
                Id = Guid.NewGuid(),
                Email = "user2@example.com",
                FullName = "User Two",
                PasswordHash = "hash",
                CurrentSessionToken = "token-user-2"
            };

            await dbContext.Users.AddRangeAsync(user1, user2);
            await dbContext.SaveChangesAsync(default);

            var validator = new SessionTokenValidator(dbContext);

            // Act
            var token1 = await validator.GetCurrentSessionTokenAsync(user1.Id);
            var token2 = await validator.GetCurrentSessionTokenAsync(user2.Id);

            // Assert
            token1.Should().Be("token-user-1");
            token2.Should().Be("token-user-2");
        }
    }
}
