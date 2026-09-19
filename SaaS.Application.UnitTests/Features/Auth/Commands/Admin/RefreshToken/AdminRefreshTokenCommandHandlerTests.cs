using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Application.Features.Auth.Commands.Admin.RefreshToken;
using SaaS.Application.UnitTests.Common;
using SaaS.Domain.Entities;
using SaaS.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SaaS.Application.UnitTests.Features.Auth.Commands.Admin.RefreshToken
{
    public class AdminRefreshTokenCommandHandlerTests
    {
        private readonly Mock<IAuthSessionService> _authSessionServiceMock;
        private readonly Mock<ILogger<AdminRefreshTokenCommandHandler>> _loggerMock;

        public AdminRefreshTokenCommandHandlerTests()
        {
            _authSessionServiceMock = new Mock<IAuthSessionService>();
            _loggerMock = new Mock<ILogger<AdminRefreshTokenCommandHandler>>();
        }

        private MockAppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<MockAppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new MockAppDbContext(options);
        }

        [Fact]
        public async Task Handle_WhenTokenIsValidAndActive_ReturnsSuccessWithAuthResponse()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var adminId = Guid.NewGuid();
            var admin = new SystemAdmin
            {
                Id = adminId,
                Email = "admin@example.com",
                FullName = "Admin Super",
                Role = "SuperAdmin",
                IsActive = true
            };
            var token = new SystemAdminRefreshTokens
            {
                AdminId = adminId,
                Token = "active-refresh-token",
                ExpDate = DateTime.UtcNow.AddDays(7),
                IsActive = true,
                Admin = admin
            };

            await db.SystmeAdmins.AddAsync(admin);
            await db.SystemAdminRefreshTokens.AddAsync(token);
            await db.SaveChangesAsync(CancellationToken.None);

            var expectedResponse = new AuthLoginResponseDto(adminId.ToString(), admin.Email, admin.FullName, admin.Role);
            _authSessionServiceMock.Setup(a => a.CreateAdminSessionAsync(adminId, admin.Email, admin.FullName, admin.Role, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            var handler = new AdminRefreshTokenCommandHandler(db, _authSessionServiceMock.Object, _loggerMock.Object);
            var command = new AdminRefreshTokenCommand("active-refresh-token");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Refresh token successful.");
            result.Data.Should().BeEquivalentTo(expectedResponse);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Handle_WhenTokenIsNullOrWhitespace_ReturnsUnauthorizedFailure(string? invalidToken)
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var handler = new AdminRefreshTokenCommandHandler(db, _authSessionServiceMock.Object, _loggerMock.Object);
            var command = new AdminRefreshTokenCommand(invalidToken!);

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
            var handler = new AdminRefreshTokenCommandHandler(db, _authSessionServiceMock.Object, _loggerMock.Object);
            var command = new AdminRefreshTokenCommand("non-existent-token");

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
            var adminId = Guid.NewGuid();
            var admin = new SystemAdmin
            {
                Id = adminId,
                Email = "admin@example.com",
                FullName = "Admin Expired",
                Role = "Admin",
                IsActive = true
            };
            var token = new SystemAdminRefreshTokens
            {
                AdminId = adminId,
                Token = "expired-token",
                ExpDate = DateTime.UtcNow.AddMinutes(-10), // Expired
                IsActive = true,
                Admin = admin
            };

            await db.SystmeAdmins.AddAsync(admin);
            await db.SystemAdminRefreshTokens.AddAsync(token);
            await db.SaveChangesAsync(CancellationToken.None);

            var handler = new AdminRefreshTokenCommandHandler(db, _authSessionServiceMock.Object, _loggerMock.Object);
            var command = new AdminRefreshTokenCommand("expired-token");

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
            var adminId = Guid.NewGuid();
            var admin = new SystemAdmin
            {
                Id = adminId,
                Email = "admin@example.com",
                FullName = "Admin Inactive Token",
                Role = "Admin",
                IsActive = true
            };
            var token = new SystemAdminRefreshTokens
            {
                AdminId = adminId,
                Token = "inactive-token",
                ExpDate = DateTime.UtcNow.AddDays(5),
                IsActive = false, // Deactivated
                Admin = admin
            };

            await db.SystmeAdmins.AddAsync(admin);
            await db.SystemAdminRefreshTokens.AddAsync(token);
            await db.SaveChangesAsync(CancellationToken.None);

            var handler = new AdminRefreshTokenCommandHandler(db, _authSessionServiceMock.Object, _loggerMock.Object);
            var command = new AdminRefreshTokenCommand("inactive-token");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ErrorType.Unauthorized);
            result.Message.Should().Be("Refresh token invalid or expired.");
        }

        [Fact]
        public async Task Handle_WhenAdminIsInactive_ReturnsUnauthorizedFailure()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var adminId = Guid.NewGuid();
            var admin = new SystemAdmin
            {
                Id = adminId,
                Email = "admin@example.com",
                FullName = "Admin Deactivated",
                Role = "Admin",
                IsActive = false // Deactivated Admin
            };
            var token = new SystemAdminRefreshTokens
            {
                AdminId = adminId,
                Token = "token-inactive-admin",
                ExpDate = DateTime.UtcNow.AddDays(5),
                IsActive = true,
                Admin = admin
            };

            await db.SystmeAdmins.AddAsync(admin);
            await db.SystemAdminRefreshTokens.AddAsync(token);
            await db.SaveChangesAsync(CancellationToken.None);

            var handler = new AdminRefreshTokenCommandHandler(db, _authSessionServiceMock.Object, _loggerMock.Object);
            var command = new AdminRefreshTokenCommand("token-inactive-admin");

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
