using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Features.Auth.Commands.Admin.Login;
using SaaS.Application.UnitTests.Common;
using SaaS.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SaaS.Application.UnitTests.Features.Auth.Commands.Admin.Login
{
    public class AdminLoginCommandHandlerTests
    {
        private readonly Mock<IPasswordHasherService> _passwordHasherMock;
        private readonly Mock<IAuthSessionService> _authSessionServiceMock;
        private readonly Mock<ILogger<AdminLoginCommandHandler>> _loggerMock;

        public AdminLoginCommandHandlerTests()
        {
            _passwordHasherMock = new Mock<IPasswordHasherService>();
            _authSessionServiceMock = new Mock<IAuthSessionService>();
            _loggerMock = new Mock<ILogger<AdminLoginCommandHandler>>();
        }

        private MockAppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<MockAppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new MockAppDbContext(options);
        }

        [Fact]
        public async Task Handle_WhenValidCredentials_ReturnsSuccessWithAuthResponse()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var adminId = Guid.NewGuid();
            var admin = new SystemAdmin
            {
                Id = adminId,
                Email = "admin@example.com",
                FullName = "Admin Name",
                PasswordHash = "valid-hash",
                Role = "SuperAdmin",
                IsActive = true
            };
            await db.SystmeAdmins.AddAsync(admin);
            await db.SaveChangesAsync(CancellationToken.None);

            _passwordHasherMock.Setup(p => p.VerifyPassword("Pass123!", "valid-hash"))
                .Returns(true);
            _passwordHasherMock.Setup(p => p.NeedsRehash("valid-hash"))
                .Returns(false);

            var expectedResponse = new AuthLoginResponseDto(adminId.ToString(), admin.Email, admin.FullName, admin.Role);
            _authSessionServiceMock.Setup(a => a.CreateAdminSessionAsync(adminId, admin.Email, admin.FullName, admin.Role, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            var handler = new AdminLoginCommandHandler(db, _passwordHasherMock.Object, _loggerMock.Object, _authSessionServiceMock.Object);
            var command = new AdminLoginCommand("admin@example.com", "Pass123!");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Admin Login successfully");
            result.Data.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task Handle_WhenAdminNotFound_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var handler = new AdminLoginCommandHandler(db, _passwordHasherMock.Object, _loggerMock.Object, _authSessionServiceMock.Object);
            var command = new AdminLoginCommand("nonexistent@example.com", "Pass123!");

            // Act
            var act = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Invalid credentials.");
        }

        [Fact]
        public async Task Handle_WhenAdminIsInactive_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var admin = new SystemAdmin
            {
                Id = Guid.NewGuid(),
                Email = "inactive@example.com",
                PasswordHash = "hash",
                IsActive = false
            };
            await db.SystmeAdmins.AddAsync(admin);
            await db.SaveChangesAsync(CancellationToken.None);

            var handler = new AdminLoginCommandHandler(db, _passwordHasherMock.Object, _loggerMock.Object, _authSessionServiceMock.Object);
            var command = new AdminLoginCommand("inactive@example.com", "Pass123!");

            // Act
            var act = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Invalid credentials.");
        }

        [Fact]
        public async Task Handle_WhenPasswordIsIncorrect_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var admin = new SystemAdmin
            {
                Id = Guid.NewGuid(),
                Email = "admin@example.com",
                PasswordHash = "valid-hash",
                IsActive = true
            };
            await db.SystmeAdmins.AddAsync(admin);
            await db.SaveChangesAsync(CancellationToken.None);

            _passwordHasherMock.Setup(p => p.VerifyPassword("WrongPass", "valid-hash"))
                .Returns(false);

            var handler = new AdminLoginCommandHandler(db, _passwordHasherMock.Object, _loggerMock.Object, _authSessionServiceMock.Object);
            var command = new AdminLoginCommand("admin@example.com", "WrongPass");

            // Act
            var act = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Invalid credentials.");
        }

        [Fact]
        public async Task Handle_WhenPasswordNeedsRehash_UpdatesAdminPasswordHash()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var admin = new SystemAdmin
            {
                Id = Guid.NewGuid(),
                Email = "admin@example.com",
                PasswordHash = "old-weak-hash",
                Role = "Admin",
                FullName = "Admin",
                IsActive = true
            };
            await db.SystmeAdmins.AddAsync(admin);
            await db.SaveChangesAsync(CancellationToken.None);

            _passwordHasherMock.Setup(p => p.VerifyPassword("Pass123!", "old-weak-hash"))
                .Returns(true);
            _passwordHasherMock.Setup(p => p.NeedsRehash("old-weak-hash"))
                .Returns(true);
            _passwordHasherMock.Setup(p => p.HashPassword("Pass123!"))
                .Returns("new-strong-hash");

            _authSessionServiceMock.Setup(a => a.CreateAdminSessionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthLoginResponseDto(admin.Id.ToString(), admin.Email, admin.FullName, admin.Role));

            var handler = new AdminLoginCommandHandler(db, _passwordHasherMock.Object, _loggerMock.Object, _authSessionServiceMock.Object);
            var command = new AdminLoginCommand("admin@example.com", "Pass123!");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            admin.PasswordHash.Should().Be("new-strong-hash");
        }

        [Fact]
        public async Task Handle_WhenRehashThrowsException_ContinuesLoginWithoutFailure()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var admin = new SystemAdmin
            {
                Id = Guid.NewGuid(),
                Email = "admin@example.com",
                PasswordHash = "hash",
                Role = "Admin",
                FullName = "Admin",
                IsActive = true
            };
            await db.SystmeAdmins.AddAsync(admin);
            await db.SaveChangesAsync(CancellationToken.None);

            _passwordHasherMock.Setup(p => p.VerifyPassword("Pass123!", "hash"))
                .Returns(true);
            _passwordHasherMock.Setup(p => p.NeedsRehash("hash"))
                .Throws(new InvalidOperationException("Rehash check failed"));

            _authSessionServiceMock.Setup(a => a.CreateAdminSessionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthLoginResponseDto(admin.Id.ToString(), admin.Email, admin.FullName, admin.Role));

            var handler = new AdminLoginCommandHandler(db, _passwordHasherMock.Object, _loggerMock.Object, _authSessionServiceMock.Object);
            var command = new AdminLoginCommand("admin@example.com", "Pass123!");

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
            var handler = new AdminLoginCommandHandler(db, _passwordHasherMock.Object, _loggerMock.Object, _authSessionServiceMock.Object);

            // Act
            var act = () => handler.Handle(null!, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("request");
        }
    }
}
