using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Application.Features.Auth.Commands.User.RefreshToken;
using SaaS.Application.UnitTests.Common;
using SaaS.Domain.Entities;
using SaaS.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using UserEntity = SaaS.Domain.Entities.User;

namespace SaaS.Application.UnitTests.Features.Auth.Commands.User.RefreshToken
{
    public class UserRefreshTokenCommandHandlerTests
    {
        private readonly Mock<IAuthSessionService> _authSessionServiceMock;
        private readonly Mock<ILogger<UserRefreshTokenCommandHandler>> _loggerMock;

        public UserRefreshTokenCommandHandlerTests()
        {
            _authSessionServiceMock = new Mock<IAuthSessionService>();
            _loggerMock = new Mock<ILogger<UserRefreshTokenCommandHandler>>();
        }

        private MockAppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<MockAppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new MockAppDbContext(options);
        }

        [Fact]
        public async Task Handle_WhenTokenIsValidAndActive_ReturnsSuccessAndUpdatesSessionToken()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var userId = Guid.NewGuid();
            var user = new UserEntity
            {
                Id = userId,
                Email = "user@example.com",
                FullName = "Normal User",
                IsDeleted = false
            };
            var token = new UserRefreshToken
            {
                UserId = userId,
                Token = "active-user-refresh-token",
                ExpDate = DateTime.UtcNow.AddDays(7),
                IsActive = true,
                User = user
            };

            await db.Users.AddAsync(user);
            await db.UserRefreshTokens.AddAsync(token);
            await db.SaveChangesAsync(CancellationToken.None);

            var expectedResponse = new AuthLoginResponseDto(userId.ToString(), user.Email, user.FullName, "User");
            _authSessionServiceMock.Setup(a => a.CreateUserSessionAsync(userId, user.Email, user.FullName, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            var handler = new UserRefreshTokenCommandHandler(db, _authSessionServiceMock.Object, _loggerMock.Object);
            var command = new UserRefreshTokenCommand("active-user-refresh-token");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Refresh token successful.");
            result.Data.Should().BeEquivalentTo(expectedResponse);

            user.CurrentSessionToken.Should().NotBeNullOrWhiteSpace();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Handle_WhenTokenIsNullOrWhitespace_ReturnsUnauthorizedFailure(string? invalidToken)
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var handler = new UserRefreshTokenCommandHandler(db, _authSessionServiceMock.Object, _loggerMock.Object);
            var command = new UserRefreshTokenCommand(invalidToken!);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ErrorType.Unauthorized);
            result.Message.Should().Be("Refresh token is missing or empty.");
        }

        [Fact]
        public async Task Handle_WhenTokenNotFoundInDb_ReturnsUnauthorizedFailure()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var handler = new UserRefreshTokenCommandHandler(db, _authSessionServiceMock.Object, _loggerMock.Object);
            var command = new UserRefreshTokenCommand("non-existent-token");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ErrorType.Unauthorized);
            result.Message.Should().Be("Refresh token invalid or expired.");
        }

        [Fact]
        public async Task Handle_WhenTokenIsExpired_ReturnsUnauthorizedFailure()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var userId = Guid.NewGuid();
            var user = new UserEntity
            {
                Id = userId,
                Email = "user@example.com",
                FullName = "User Expired",
                IsDeleted = false
            };
            var token = new UserRefreshToken
            {
                UserId = userId,
                Token = "expired-token",
                ExpDate = DateTime.UtcNow.AddMinutes(-5),
                IsActive = true,
                User = user
            };

            await db.Users.AddAsync(user);
            await db.UserRefreshTokens.AddAsync(token);
            await db.SaveChangesAsync(CancellationToken.None);

            var handler = new UserRefreshTokenCommandHandler(db, _authSessionServiceMock.Object, _loggerMock.Object);
            var command = new UserRefreshTokenCommand("expired-token");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ErrorType.Unauthorized);
            result.Message.Should().Be("Refresh token invalid or expired.");
        }

        [Fact]
        public async Task Handle_WhenTokenIsNotActive_ReturnsUnauthorizedFailure()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var userId = Guid.NewGuid();
            var user = new UserEntity
            {
                Id = userId,
                Email = "user@example.com",
                FullName = "User Inactive",
                IsDeleted = false
            };
            var token = new UserRefreshToken
            {
                UserId = userId,
                Token = "inactive-token",
                ExpDate = DateTime.UtcNow.AddDays(5),
                IsActive = false,
                User = user
            };

            await db.Users.AddAsync(user);
            await db.UserRefreshTokens.AddAsync(token);
            await db.SaveChangesAsync(CancellationToken.None);

            var handler = new UserRefreshTokenCommandHandler(db, _authSessionServiceMock.Object, _loggerMock.Object);
            var command = new UserRefreshTokenCommand("inactive-token");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ErrorType.Unauthorized);
            result.Message.Should().Be("Refresh token invalid or expired.");
        }

        [Fact]
        public async Task Handle_WhenUserIsDeleted_ReturnsUnauthorizedFailure()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var userId = Guid.NewGuid();
            var user = new UserEntity
            {
                Id = userId,
                Email = "user@example.com",
                FullName = "Deleted User",
                IsDeleted = true // Deleted
            };
            var token = new UserRefreshToken
            {
                UserId = userId,
                Token = "token-deleted-user",
                ExpDate = DateTime.UtcNow.AddDays(5),
                IsActive = true,
                User = user
            };

            await db.Users.AddAsync(user);
            await db.UserRefreshTokens.AddAsync(token);
            await db.SaveChangesAsync(CancellationToken.None);

            var handler = new UserRefreshTokenCommandHandler(db, _authSessionServiceMock.Object, _loggerMock.Object);
            var command = new UserRefreshTokenCommand("token-deleted-user");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ErrorType.Unauthorized);
            result.Message.Should().Be("Refresh token invalid or expired.");
        }
    }
}
