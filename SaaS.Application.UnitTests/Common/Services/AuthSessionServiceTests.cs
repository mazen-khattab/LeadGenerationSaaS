using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Services;
using SaaS.Application.Common.Settings;
using SaaS.Application.UnitTests.Common;
using SaaS.Domain.Entities;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SaaS.Application.UnitTests.Common.Services
{
    public class AuthSessionServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<IOptionsSnapshot<SecuritySettings>> _securityOptionsMock;
        private readonly Mock<ILogger<AuthSessionService>> _loggerMock;
        private readonly SecuritySettings _securitySettings;

        public AuthSessionServiceTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _tokenServiceMock = new Mock<ITokenService>();
            _securityOptionsMock = new Mock<IOptionsSnapshot<SecuritySettings>>();
            _loggerMock = new Mock<ILogger<AuthSessionService>>();

            _securitySettings = new SecuritySettings
            {
                RefreshTokenExpirationDays = 7,
                AccessTokenExpirationMinutes = 15
            };
            _securityOptionsMock.Setup(o => o.Value).Returns(_securitySettings);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        private TestAuthDbContext CreateDbContext(Func<CancellationToken, Task<int>>? onSaveChangesAsync = null)
        {
            var options = new DbContextOptionsBuilder<MockAppDbContext>()
                .UseSqlite(_connection)
                .Options;

            var context = new TestAuthDbContext(options, onSaveChangesAsync);
            context.Database.EnsureCreated();
            return context;
        }

        private static SqlException CreateSqlException(int errorNumber)
        {
            var collection = (SqlErrorCollection)RuntimeHelpers.GetUninitializedObject(typeof(SqlErrorCollection));
            var error = (SqlError)RuntimeHelpers.GetUninitializedObject(typeof(SqlError));

            var numberField = typeof(SqlError).GetField("_number", BindingFlags.NonPublic | BindingFlags.Instance)
                           ?? typeof(SqlError).GetField("number", BindingFlags.NonPublic | BindingFlags.Instance);
            numberField?.SetValue(error, errorNumber);

            var errorsListField = typeof(SqlErrorCollection).GetField("_errors", BindingFlags.NonPublic | BindingFlags.Instance)
                               ?? typeof(SqlErrorCollection).GetField("errors", BindingFlags.NonPublic | BindingFlags.Instance);

            if (errorsListField != null)
            {
                var list = (IList)Activator.CreateInstance(errorsListField.FieldType)!;
                list.Add(error);
                errorsListField.SetValue(collection, list);
            }
            else
            {
                var addMethod = typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance);
                addMethod?.Invoke(collection, new object[] { error });
            }

            var sqlException = (SqlException)RuntimeHelpers.GetUninitializedObject(typeof(SqlException));
            var errorsField = typeof(SqlException).GetField("_errors", BindingFlags.NonPublic | BindingFlags.Instance)
                           ?? typeof(SqlException).GetField("errors", BindingFlags.NonPublic | BindingFlags.Instance);
            errorsField?.SetValue(sqlException, collection);

            return sqlException;
        }

        [Fact]
        public async Task CreateUserSessionAsync_WhenValidRequest_PersistsTokenAndSetsCookies()
        {
            // Arrange
            using var db = CreateDbContext();
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Email = "user@example.com",
                FullName = "John Doe",
                PasswordHash = "hash"
            };
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync(CancellationToken.None);

            var expectedAccessToken = "access-token-123";
            var expectedRefreshToken = "refresh-token-abc";
            var sessionToken = "session-token-xyz";

            _tokenServiceMock.Setup(t => t.GenerateAccessToken(userId, user.Email, sessionToken))
                .Returns(expectedAccessToken);
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
                .Returns(expectedRefreshToken);

            var service = new AuthSessionService(db, _tokenServiceMock.Object, _securityOptionsMock.Object, _loggerMock.Object);

            // Act
            var response = await service.CreateUserSessionAsync(userId, user.Email, user.FullName, sessionToken, CancellationToken.None);

            // Assert
            response.Should().NotBeNull();
            response.UserId.Should().Be(userId.ToString());
            response.Email.Should().Be(user.Email);
            response.Name.Should().Be(user.FullName);
            response.Role.Should().Be("User");

            _tokenServiceMock.Verify(t => t.SetAuthCookies(expectedAccessToken, expectedRefreshToken), Times.Once);

            var persistedToken = await db.UserRefreshTokens.FirstOrDefaultAsync(t => t.UserId == userId);
            persistedToken.Should().NotBeNull();
            persistedToken!.Token.Should().Be(expectedRefreshToken);
            persistedToken.IsActive.Should().BeTrue();
            persistedToken.ExpDate.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task CreateUserSessionAsync_WhenPreviousActiveTokensExist_RevokesOldToken()
        {
            // Arrange
            using var db = CreateDbContext();
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Email = "user@example.com",
                FullName = "John Doe",
                PasswordHash = "hash"
            };
            await db.Users.AddAsync(user);

            var oldToken = new UserRefreshToken
            {
                UserId = userId,
                Token = "old-token-val",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                ExpDate = DateTime.UtcNow.AddDays(6),
                IsActive = true
            };
            await db.UserRefreshTokens.AddAsync(oldToken);
            await db.SaveChangesAsync(CancellationToken.None);

            _tokenServiceMock.Setup(t => t.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("new-access-token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
                .Returns("new-refresh-token");

            var service = new AuthSessionService(db, _tokenServiceMock.Object, _securityOptionsMock.Object, _loggerMock.Object);

            // Act
            await service.CreateUserSessionAsync(userId, user.Email, user.FullName, "session", CancellationToken.None);

            // Assert
            var tokens = await db.UserRefreshTokens.AsNoTracking().Where(t => t.UserId == userId).ToListAsync();
            tokens.Should().HaveCount(2);

            var revokedToken = tokens.First(t => t.Token == "old-token-val");
            revokedToken.IsActive.Should().BeFalse();
            revokedToken.ExpDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            var activeToken = tokens.First(t => t.Token == "new-refresh-token");
            activeToken.IsActive.Should().BeTrue();
        }

        [Theory]
        [InlineData(2601)]
        [InlineData(2627)]
        public async Task CreateUserSessionAsync_WhenUniqueConstraintViolationOccurs_DoesNotThrowAndDoesNotSetCookies(int sqlErrorNumber)
        {
            // Arrange
            var sqlEx = CreateSqlException(sqlErrorNumber);
            var dbEx = new DbUpdateException("Duplicate key violation", sqlEx);

            using var db = CreateDbContext(onSaveChangesAsync: _ => throw dbEx);
            var userId = Guid.NewGuid();

            _tokenServiceMock.Setup(t => t.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("access-token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
                .Returns("refresh-token");

            var service = new AuthSessionService(db, _tokenServiceMock.Object, _securityOptionsMock.Object, _loggerMock.Object);

            // Act
            var response = await service.CreateUserSessionAsync(userId, "user@example.com", "John Doe", "session", CancellationToken.None);

            // Assert
            response.Should().NotBeNull();
            response.UserId.Should().Be(userId.ToString());
            response.Role.Should().Be("User");

            // Cookies must NOT be set when race condition is detected
            _tokenServiceMock.Verify(t => t.SetAuthCookies(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CreateUserSessionAsync_WhenGeneralDatabaseErrorOccurs_RollsBackAndRethrows()
        {
            // Arrange
            using var db = CreateDbContext(onSaveChangesAsync: _ => throw new InvalidOperationException("DB connection dropped"));
            var userId = Guid.NewGuid();

            _tokenServiceMock.Setup(t => t.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("access-token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
                .Returns("refresh-token");

            var service = new AuthSessionService(db, _tokenServiceMock.Object, _securityOptionsMock.Object, _loggerMock.Object);

            // Act
            var act = () => service.CreateUserSessionAsync(userId, "user@example.com", "John Doe", "session", CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("DB connection dropped");

            _tokenServiceMock.Verify(t => t.SetAuthCookies(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CreateAdminSessionAsync_WhenValidRequest_PersistsTokenAndSetsCookies()
        {
            // Arrange
            using var db = CreateDbContext();
            var adminId = Guid.NewGuid();
            var admin = new SystemAdmin
            {
                Id = adminId,
                Email = "admin@example.com",
                FullName = "Admin User",
                PasswordHash = "hash",
                Role = "SuperAdmin"
            };
            await db.SystmeAdmins.AddAsync(admin);
            await db.SaveChangesAsync(CancellationToken.None);

            var expectedAccessToken = "admin-access-token";
            var expectedRefreshToken = "admin-refresh-token";

            _tokenServiceMock.Setup(t => t.GenerateAdminAccessToken(adminId, admin.Email, admin.Role))
                .Returns(expectedAccessToken);
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
                .Returns(expectedRefreshToken);

            var service = new AuthSessionService(db, _tokenServiceMock.Object, _securityOptionsMock.Object, _loggerMock.Object);

            // Act
            var response = await service.CreateAdminSessionAsync(adminId, admin.Email, admin.FullName, admin.Role, CancellationToken.None);

            // Assert
            response.Should().NotBeNull();
            response.UserId.Should().Be(adminId.ToString());
            response.Email.Should().Be(admin.Email);
            response.Name.Should().Be(admin.FullName);
            response.Role.Should().Be(admin.Role);

            _tokenServiceMock.Verify(t => t.SetAuthCookies(expectedAccessToken, expectedRefreshToken), Times.Once);

            var persistedToken = await db.SystemAdminRefreshTokens.FirstOrDefaultAsync(t => t.AdminId == adminId);
            persistedToken.Should().NotBeNull();
            persistedToken!.Token.Should().Be(expectedRefreshToken);
            persistedToken.IsActive.Should().BeTrue();
            persistedToken.ExpDate.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task CreateAdminSessionAsync_WhenPreviousActiveTokensExist_RevokesOldToken()
        {
            // Arrange
            using var db = CreateDbContext();
            var adminId = Guid.NewGuid();
            var admin = new SystemAdmin
            {
                Id = adminId,
                Email = "admin@example.com",
                FullName = "Admin User",
                PasswordHash = "hash",
                Role = "Admin"
            };
            await db.SystmeAdmins.AddAsync(admin);

            var oldToken = new SystemAdminRefreshTokens
            {
                AdminId = adminId,
                Token = "old-admin-token",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                ExpDate = DateTime.UtcNow.AddDays(5),
                IsActive = true
            };
            await db.SystemAdminRefreshTokens.AddAsync(oldToken);
            await db.SaveChangesAsync(CancellationToken.None);

            _tokenServiceMock.Setup(t => t.GenerateAdminAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("new-admin-access");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
                .Returns("new-admin-refresh");

            var service = new AuthSessionService(db, _tokenServiceMock.Object, _securityOptionsMock.Object, _loggerMock.Object);

            // Act
            await service.CreateAdminSessionAsync(adminId, admin.Email, admin.FullName, admin.Role, CancellationToken.None);

            // Assert
            var tokens = await db.SystemAdminRefreshTokens.AsNoTracking().Where(t => t.AdminId == adminId).ToListAsync();
            tokens.Should().HaveCount(2);

            var revokedToken = tokens.First(t => t.Token == "old-admin-token");
            revokedToken.IsActive.Should().BeFalse();
            revokedToken.ExpDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            var activeToken = tokens.First(t => t.Token == "new-admin-refresh");
            activeToken.IsActive.Should().BeTrue();
        }

        [Theory]
        [InlineData(2601)]
        [InlineData(2627)]
        public async Task CreateAdminSessionAsync_WhenUniqueConstraintViolationOccurs_DoesNotThrowAndDoesNotSetCookies(int sqlErrorNumber)
        {
            // Arrange
            var sqlEx = CreateSqlException(sqlErrorNumber);
            var dbEx = new DbUpdateException("Duplicate key violation", sqlEx);

            using var db = CreateDbContext(onSaveChangesAsync: _ => throw dbEx);
            var adminId = Guid.NewGuid();

            _tokenServiceMock.Setup(t => t.GenerateAdminAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("admin-access");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
                .Returns("admin-refresh");

            var service = new AuthSessionService(db, _tokenServiceMock.Object, _securityOptionsMock.Object, _loggerMock.Object);

            // Act
            var response = await service.CreateAdminSessionAsync(adminId, "admin@example.com", "Admin User", "Admin", CancellationToken.None);

            // Assert
            response.Should().NotBeNull();
            response.UserId.Should().Be(adminId.ToString());
            response.Role.Should().Be("Admin");

            _tokenServiceMock.Verify(t => t.SetAuthCookies(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CreateAdminSessionAsync_WhenGeneralDatabaseErrorOccurs_RollsBackAndRethrows()
        {
            // Arrange
            using var db = CreateDbContext(onSaveChangesAsync: _ => throw new InvalidOperationException("DB timeout"));
            var adminId = Guid.NewGuid();

            _tokenServiceMock.Setup(t => t.GenerateAdminAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("admin-access");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
                .Returns("admin-refresh");

            var service = new AuthSessionService(db, _tokenServiceMock.Object, _securityOptionsMock.Object, _loggerMock.Object);

            // Act
            var act = () => service.CreateAdminSessionAsync(adminId, "admin@example.com", "Admin User", "Admin", CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("DB timeout");

            _tokenServiceMock.Verify(t => t.SetAuthCookies(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        private class TestAuthDbContext : MockAppDbContext
        {
            private readonly Func<CancellationToken, Task<int>>? _onSaveChangesAsync;

            public TestAuthDbContext(DbContextOptions<MockAppDbContext> options, Func<CancellationToken, Task<int>>? onSaveChangesAsync)
                : base(options)
            {
                _onSaveChangesAsync = onSaveChangesAsync;
            }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken)
            {
                if (_onSaveChangesAsync != null)
                {
                    return _onSaveChangesAsync(cancellationToken);
                }

                return base.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
