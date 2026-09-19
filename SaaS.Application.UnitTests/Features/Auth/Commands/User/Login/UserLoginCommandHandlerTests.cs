using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Features.Auth.Commands.User.Login;
using SaaS.Application.UnitTests.Common;
using SaaS.Domain.Entities;
using SaaS.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using UserEntity = SaaS.Domain.Entities.User;

namespace SaaS.Application.UnitTests.Features.Auth.Commands.User.Login
{
    public class UserLoginCommandHandlerTests
    {
        private readonly Mock<IPasswordHasherService> _passwordHasherMock;
        private readonly Mock<IAuthSessionService> _authSessionServiceMock;
        private readonly Mock<ILogger<UserLoginCommandHandler>> _loggerMock;

        public UserLoginCommandHandlerTests()
        {
            _passwordHasherMock = new Mock<IPasswordHasherService>();
            _authSessionServiceMock = new Mock<IAuthSessionService>();
            _loggerMock = new Mock<ILogger<UserLoginCommandHandler>>();
        }

        private MockAppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<MockAppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new MockAppDbContext(options);
        }

        [Fact]
        public async Task Handle_WhenValidCredentials_ReturnsSuccessAndUpdatesSessionToken()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var userId = Guid.NewGuid();
            var user = new UserEntity
            {
                Id = userId,
                Email = "user@example.com",
                FullName = "Normal User",
                PasswordHash = "correct-hash",
                IsDeleted = false
            };
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync(CancellationToken.None);

            _passwordHasherMock.Setup(p => p.VerifyPassword("Pass123!", "correct-hash"))
                .Returns(true);
            _passwordHasherMock.Setup(p => p.NeedsRehash("correct-hash"))
                .Returns(false);

            var expectedResponse = new AuthLoginResponseDto(userId.ToString(), user.Email, user.FullName, "User");
            _authSessionServiceMock.Setup(a => a.CreateUserSessionAsync(userId, user.Email, user.FullName, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            var handler = new UserLoginCommandHandler(db, _passwordHasherMock.Object, _loggerMock.Object, _authSessionServiceMock.Object);
            var command = new UserLoginCommand("user@example.com", "Pass123!");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Login successful.");
            result.Data.Should().BeEquivalentTo(expectedResponse);

            user.CurrentSessionToken.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Handle_WhenUserNotFound_ReturnsInvalidCredentialsFailure()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var handler = new UserLoginCommandHandler(db, _passwordHasherMock.Object, _loggerMock.Object, _authSessionServiceMock.Object);
            var command = new UserLoginCommand("missing@example.com", "Pass123!");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ErrorType.InvalidCredentials);
            result.Message.Should().Be("Invalid credentials.");
        }

        [Fact]
        public async Task Handle_WhenUserIsDeleted_ReturnsInvalidCredentialsFailure()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var user = new UserEntity
            {
                Id = Guid.NewGuid(),
                Email = "deleted@example.com",
                PasswordHash = "hash",
                IsDeleted = true // Deleted
            };
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync(CancellationToken.None);

            var handler = new UserLoginCommandHandler(db, _passwordHasherMock.Object, _loggerMock.Object, _authSessionServiceMock.Object);
            var command = new UserLoginCommand("deleted@example.com", "Pass123!");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ErrorType.InvalidCredentials);
            result.Message.Should().Be("Invalid credentials.");
        }

        [Fact]
        public async Task Handle_WhenPasswordIsIncorrect_ReturnsInvalidCredentialsFailure()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var user = new UserEntity
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                PasswordHash = "hash",
                IsDeleted = false
            };
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync(CancellationToken.None);

            _passwordHasherMock.Setup(p => p.VerifyPassword("WrongPassword", "hash"))
                .Returns(false);

            var handler = new UserLoginCommandHandler(db, _passwordHasherMock.Object, _loggerMock.Object, _authSessionServiceMock.Object);
            var command = new UserLoginCommand("user@example.com", "WrongPassword");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ErrorType.InvalidCredentials);
            result.Message.Should().Be("Invalid credentials.");
        }

        [Fact]
        public async Task Handle_WhenPasswordNeedsRehash_UpdatesPasswordHash()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var user = new UserEntity
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                FullName = "Normal User",
                PasswordHash = "weak-legacy-hash",
                IsDeleted = false
            };
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync(CancellationToken.None);

            _passwordHasherMock.Setup(p => p.VerifyPassword("Pass123!", "weak-legacy-hash"))
                .Returns(true);
            _passwordHasherMock.Setup(p => p.NeedsRehash("weak-legacy-hash"))
                .Returns(true);
            _passwordHasherMock.Setup(p => p.HashPassword("Pass123!"))
                .Returns("new-strong-hash");

            _authSessionServiceMock.Setup(a => a.CreateUserSessionAsync(user.Id, user.Email, user.FullName, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthLoginResponseDto(user.Id.ToString(), user.Email, user.FullName, "User"));

            var handler = new UserLoginCommandHandler(db, _passwordHasherMock.Object, _loggerMock.Object, _authSessionServiceMock.Object);
            var command = new UserLoginCommand("user@example.com", "Pass123!");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            user.PasswordHash.Should().Be("new-strong-hash");
        }

        [Fact]
        public async Task Handle_WhenRehashThrows_ContinuesLoginSuccessfully()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var user = new UserEntity
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                FullName = "Normal User",
                PasswordHash = "hash",
                IsDeleted = false
            };
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync(CancellationToken.None);

            _passwordHasherMock.Setup(p => p.VerifyPassword("Pass123!", "hash"))
                .Returns(true);
            _passwordHasherMock.Setup(p => p.NeedsRehash("hash"))
                .Throws(new InvalidOperationException("Rehash error"));

            _authSessionServiceMock.Setup(a => a.CreateUserSessionAsync(user.Id, user.Email, user.FullName, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthLoginResponseDto(user.Id.ToString(), user.Email, user.FullName, "User"));

            var handler = new UserLoginCommandHandler(db, _passwordHasherMock.Object, _loggerMock.Object, _authSessionServiceMock.Object);
            var command = new UserLoginCommand("user@example.com", "Pass123!");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_WhenRequestIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var handler = new UserLoginCommandHandler(db, _passwordHasherMock.Object, _loggerMock.Object, _authSessionServiceMock.Object);

            // Act
            var act = () => handler.Handle(null!, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("request");
        }
    }
}
