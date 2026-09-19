using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Application.Features.Runs.Commands.Create;
using SaaS.Application.UnitTests.Common;
using SaaS.Domain.Entities;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SaaS.Application.UnitTests.Features.Runs.Commands.Create
{
    public class CreateRunCommandHandlerTests
    {
        private readonly Mock<IEncryptionService> _encryptionServiceMock;
        private readonly Mock<INetworkClient> _networkClientMock;
        private readonly Mock<IN8nWebhookResolver> _webhookResolverMock;
        private readonly Mock<IUserBotService> _userBotServiceMock;
        private readonly Mock<ILogger<CreateRunCommandHandler>> _loggerMock;

        public CreateRunCommandHandlerTests()
        {
            _encryptionServiceMock = new Mock<IEncryptionService>();
            _networkClientMock = new Mock<INetworkClient>();
            _webhookResolverMock = new Mock<IN8nWebhookResolver>();
            _userBotServiceMock = new Mock<IUserBotService>();
            _loggerMock = new Mock<ILogger<CreateRunCommandHandler>>();
        }

        private MockAppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<MockAppDbContext>()
                .UseInMemoryDatabase(databaseName: $"CreateRunDb_{Guid.NewGuid()}")
                .Options;

            return new MockAppDbContext(options);
        }

        private void SetupValidMocks(int botId, string uiCode = "BOT_LEAD_GEN")
        {
            _encryptionServiceMock
                .Setup(e => e.Decrypt(It.IsAny<string>()))
                .Returns("[{\"name\":\"cookie1\",\"value\":\"val1\"}]");

            _webhookResolverMock
                .Setup(w => w.GetWebhookUrl(uiCode))
                .Returns("https://n8n.example.com/webhook/runs");

            _networkClientMock
                .Setup(n => n.PostJsonAsync(It.IsAny<string>(), It.IsAny<object>(), ExternalSystem.N8n, It.IsAny<CancellationToken>()))
                .ReturnsAsync(NetworkResult.Ok(200, "{\"status\":\"ok\"}"));
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

            var handler = new CreateRunCommandHandler(
                dbContext, _encryptionServiceMock.Object, _networkClientMock.Object,
                _webhookResolverMock.Object, _userBotServiceMock.Object, _loggerMock.Object);

            var command = new CreateRunCommand(userId, new CreateRunDto(botId, 1, null, "{}"));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.NotFound, result.ErrorType);
            Assert.Equal("User or bot not found", result.Message);
        }

        [Fact]
        public async Task Handle_WhenConnectedAccountNotFound_ShouldReturnNotFoundFailure()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new CreateRunCommandHandler(
                dbContext, _encryptionServiceMock.Object, _networkClientMock.Object,
                _webhookResolverMock.Object, _userBotServiceMock.Object, _loggerMock.Object);

            var command = new CreateRunCommand(userId, new CreateRunDto(botId, 999, null, "{}"));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.NotFound, result.ErrorType);
            Assert.Equal("Connected account not found.", result.Message);
        }

        [Theory]
        [InlineData("Busy")]
        [InlineData("Cooling_Down")]
        [InlineData("Error")]
        public async Task Handle_WhenConnectedAccountIsNotActive_ShouldReturnValidationError(string invalidStatus)
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;
            var accountId = 10;

            var bot = new Bot { Id = botId, Name = "Test Bot", UiModuleCode = "UI_BOT" };
            var account = new ConnectedAccount
            {
                Id = accountId,
                UserId = userId,
                BotId = botId,
                Bot = bot,
                Status = invalidStatus
            };

            await dbContext.Bots.AddAsync(bot);
            await dbContext.ConnectedAccounts.AddAsync(account);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new CreateRunCommandHandler(
                dbContext, _encryptionServiceMock.Object, _networkClientMock.Object,
                _webhookResolverMock.Object, _userBotServiceMock.Object, _loggerMock.Object);

            var command = new CreateRunCommand(userId, new CreateRunDto(botId, accountId, null, "{}"));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.ValidationError, result.ErrorType);
            Assert.Contains($"Account is currently {invalidStatus} and cannot be used.", result.Message);
        }

        [Fact]
        public async Task Handle_WhenPreviousRunIsStillInProgress_ShouldReturnTooManyRequestsFailure()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;
            var accountId = 10;

            var bot = new Bot { Id = botId, Name = "Test Bot", UiModuleCode = "UI_BOT", CooldownMinutes = 60 };
            var account = new ConnectedAccount
            {
                Id = accountId,
                UserId = userId,
                BotId = botId,
                Bot = bot,
                Status = AccountStatus.ACTIVE.ToDbString()
            };

            // Previous run with EndedAt == null (still running)
            var activeRun = new Run
            {
                Id = 1,
                UserId = userId,
                BotId = botId,
                AccountId = accountId,
                StartedAt = DateTime.UtcNow.AddMinutes(-30),
                EndedAt = null,
                Status = RunStatus.RUNNING.ToDbString()
            };

            await dbContext.Bots.AddAsync(bot);
            await dbContext.ConnectedAccounts.AddAsync(account);
            await dbContext.Runs.AddAsync(activeRun);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new CreateRunCommandHandler(
                dbContext, _encryptionServiceMock.Object, _networkClientMock.Object,
                _webhookResolverMock.Object, _userBotServiceMock.Object, _loggerMock.Object);

            var command = new CreateRunCommand(userId, new CreateRunDto(botId, accountId, null, "{}"));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.TooManyRequests, result.ErrorType);
            Assert.Equal("The previous run is still in progress. Please wait until it completes.", result.Message);
        }

        [Fact]
        public async Task Handle_WhenCooldownPeriodIsActive_ShouldReturnTooManyRequestsFailure()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;
            var accountId = 10;

            var bot = new Bot { Id = botId, Name = "Test Bot", UiModuleCode = "UI_BOT", CooldownMinutes = 60 };
            var account = new ConnectedAccount
            {
                Id = accountId,
                UserId = userId,
                BotId = botId,
                Bot = bot,
                Status = AccountStatus.ACTIVE.ToDbString()
            };

            // Previous run ended 10 minutes ago, cooldown is 60 minutes -> 50 minutes remaining
            var lastRun = new Run
            {
                Id = 1,
                UserId = userId,
                BotId = botId,
                AccountId = accountId,
                StartedAt = DateTime.UtcNow.AddMinutes(-40),
                EndedAt = DateTime.UtcNow.AddMinutes(-10),
                Status = RunStatus.COMPLETED.ToDbString()
            };

            await dbContext.Bots.AddAsync(bot);
            await dbContext.ConnectedAccounts.AddAsync(account);
            await dbContext.Runs.AddAsync(lastRun);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new CreateRunCommandHandler(
                dbContext, _encryptionServiceMock.Object, _networkClientMock.Object,
                _webhookResolverMock.Object, _userBotServiceMock.Object, _loggerMock.Object);

            var command = new CreateRunCommand(userId, new CreateRunDto(botId, accountId, null, "{}"));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.TooManyRequests, result.ErrorType);
            Assert.Contains("Cooldown active. You can start a new run after", result.Message);
        }

        [Fact]
        public async Task Handle_WhenTargetGroupIdProvidedButNotFound_ShouldReturnNotFoundFailure()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;
            var accountId = 10;
            var nonExistentGroupId = 999;

            var bot = new Bot { Id = botId, Name = "Test Bot", UiModuleCode = "UI_BOT", CooldownMinutes = 10 };
            var account = new ConnectedAccount
            {
                Id = accountId,
                UserId = userId,
                BotId = botId,
                Bot = bot,
                Status = AccountStatus.ACTIVE.ToDbString()
            };

            await dbContext.Bots.AddAsync(bot);
            await dbContext.ConnectedAccounts.AddAsync(account);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new CreateRunCommandHandler(
                dbContext, _encryptionServiceMock.Object, _networkClientMock.Object,
                _webhookResolverMock.Object, _userBotServiceMock.Object, _loggerMock.Object);

            var command = new CreateRunCommand(userId, new CreateRunDto(botId, accountId, nonExistentGroupId, "{}"));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.NotFound, result.ErrorType);
            Assert.Equal("Target group not found.", result.Message);
        }

        [Fact]
        public async Task Handle_WhenUserSettingsNotFound_ShouldReturnNotFoundFailure()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;
            var accountId = 10;

            var bot = new Bot { Id = botId, Name = "Test Bot", UiModuleCode = "UI_BOT", CooldownMinutes = 10 };
            var account = new ConnectedAccount
            {
                Id = accountId,
                UserId = userId,
                BotId = botId,
                Bot = bot,
                Status = AccountStatus.ACTIVE.ToDbString()
            };

            await dbContext.Bots.AddAsync(bot);
            await dbContext.ConnectedAccounts.AddAsync(account);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new CreateRunCommandHandler(
                dbContext, _encryptionServiceMock.Object, _networkClientMock.Object,
                _webhookResolverMock.Object, _userBotServiceMock.Object, _loggerMock.Object);

            var command = new CreateRunCommand(userId, new CreateRunDto(botId, accountId, null, "{}"));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.NotFound, result.ErrorType);
            Assert.Equal("Some user settings is missing.", result.Message);
        }

        [Theory]
        [InlineData(null, "Pitch")]
        [InlineData("", "Pitch")]
        [InlineData("Company", null)]
        [InlineData("Company", "")]
        public async Task Handle_WhenCompanyInfoIsIncomplete_ShouldReturnNotFoundFailure(string? companyName, string? companyPitch)
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;
            var accountId = 10;

            var bot = new Bot { Id = botId, Name = "Test Bot", UiModuleCode = "UI_BOT", CooldownMinutes = 10 };
            var account = new ConnectedAccount
            {
                Id = accountId,
                UserId = userId,
                BotId = botId,
                Bot = bot,
                Status = AccountStatus.ACTIVE.ToDbString()
            };

            var userSetting = new UserSetting
            {
                Id = 1,
                UserId = userId,
                CompanyName = companyName,
                CompanyPitch = companyPitch
            };

            await dbContext.Bots.AddAsync(bot);
            await dbContext.ConnectedAccounts.AddAsync(account);
            await dbContext.UserSettings.AddAsync(userSetting);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new CreateRunCommandHandler(
                dbContext, _encryptionServiceMock.Object, _networkClientMock.Object,
                _webhookResolverMock.Object, _userBotServiceMock.Object, _loggerMock.Object);

            var command = new CreateRunCommand(userId, new CreateRunDto(botId, accountId, null, "{}"));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.NotFound, result.ErrorType);
            Assert.Equal("Some user settings is missing.", result.Message);
        }

        [Fact]
        public async Task Handle_WhenCookiesHaveExpired_ShouldReturnValidationError()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;
            var accountId = 10;

            var bot = new Bot { Id = botId, Name = "Test Bot", UiModuleCode = "UI_BOT", CooldownMinutes = 10 };
            var account = new ConnectedAccount
            {
                Id = accountId,
                UserId = userId,
                BotId = botId,
                Bot = bot,
                Status = AccountStatus.ACTIVE.ToDbString(),
                Cookie = new ConnectedAccountCookie
                {
                    AccountId = accountId,
                    EncryptedCookies = "expired-cookie",
                    CookiesExpireDate = DateTime.UtcNow.AddMinutes(-5) // Expired
                }
            };

            var userSetting = new UserSetting
            {
                Id = 1,
                UserId = userId,
                CompanyName = "Tech Corp",
                CompanyPitch = "Our Pitch"
            };

            await dbContext.Bots.AddAsync(bot);
            await dbContext.ConnectedAccounts.AddAsync(account);
            await dbContext.UserSettings.AddAsync(userSetting);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new CreateRunCommandHandler(
                dbContext, _encryptionServiceMock.Object, _networkClientMock.Object,
                _webhookResolverMock.Object, _userBotServiceMock.Object, _loggerMock.Object);

            var command = new CreateRunCommand(userId, new CreateRunDto(botId, accountId, null, "{}"));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.ValidationError, result.ErrorType);
            Assert.Equal("Your account cookies has been expired. Pls refresh it.", result.Message);
        }

        [Fact]
        public async Task Handle_WhenAllConditionsMet_ShouldCreateRunAndDispatchExternalCallSuccessfully()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;
            var accountId = 10;
            var groupId = 25;

            var bot = new Bot { Id = botId, Name = "Lead Bot", UiModuleCode = "BOT_FACEBOOK", CooldownMinutes = 10 };
            var account = new ConnectedAccount
            {
                Id = accountId,
                UserId = userId,
                BotId = botId,
                Bot = bot,
                Status = AccountStatus.ACTIVE.ToDbString(),
                Cookie = new ConnectedAccountCookie
                {
                    AccountId = accountId,
                    EncryptedCookies = "encrypted-secret",
                    CookiesExpireDate = DateTime.UtcNow.AddDays(10)
                }
            };

            var targetGroup = new TargetGroup
            {
                Id = groupId,
                UserId = userId,
                BotId = botId,
                GroupName = "Dentists"
            };

            var userSetting = new UserSetting
            {
                Id = 1,
                UserId = userId,
                CompanyName = "Dentist Outreach Inc",
                CompanyPitch = "We provide dental leads"
            };

            // Previous run completed long ago, cooldown satisfied
            var previousRun = new Run
            {
                Id = 1,
                UserId = userId,
                BotId = botId,
                AccountId = accountId,
                StartedAt = DateTime.UtcNow.AddHours(-2),
                EndedAt = DateTime.UtcNow.AddHours(-1),
                Status = RunStatus.COMPLETED.ToDbString()
            };

            await dbContext.Bots.AddAsync(bot);
            await dbContext.ConnectedAccounts.AddAsync(account);
            await dbContext.TargetGroups.AddAsync(targetGroup);
            await dbContext.UserSettings.AddAsync(userSetting);
            await dbContext.Runs.AddAsync(previousRun);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            SetupValidMocks(botId, "BOT_FACEBOOK");

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new CreateRunCommandHandler(
                dbContext, _encryptionServiceMock.Object, _networkClientMock.Object,
                _webhookResolverMock.Object, _userBotServiceMock.Object, _loggerMock.Object);

            var command = new CreateRunCommand(userId, new CreateRunDto(botId, accountId, groupId, "{\"maxLeads\": 50}"));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Data > 0);
            Assert.Equal("Run started successfully.", result.Message);

            var run = await dbContext.Runs.FindAsync(result.Data);
            Assert.NotNull(run);
            Assert.Equal(userId, run.UserId);
            Assert.Equal(botId, run.BotId);
            Assert.Equal(accountId, run.AccountId);
            Assert.Equal(groupId, run.GroupId);
            Assert.Equal(RunStatus.RUNNING.ToDbString(), run.Status);
            Assert.Equal("{\"maxLeads\": 50}", run.InfoJson);

            // Account status should be BUSY
            var updatedAccount = await dbContext.ConnectedAccounts.FindAsync(accountId);
            Assert.NotNull(updatedAccount);
            Assert.Equal(AccountStatus.BUSY.ToDbString(), updatedAccount.Status);

            _networkClientMock.Verify(n =>
                n.PostJsonAsync("https://n8n.example.com/webhook/runs", It.IsAny<object>(), ExternalSystem.N8n, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenTargetGroupIdIsNull_ShouldCreateRunWithoutGroupId()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;
            var accountId = 10;

            var bot = new Bot { Id = botId, Name = "Lead Bot", UiModuleCode = "BOT_FACEBOOK", CooldownMinutes = 10 };
            var account = new ConnectedAccount
            {
                Id = accountId,
                UserId = userId,
                BotId = botId,
                Bot = bot,
                Status = AccountStatus.ACTIVE.ToDbString(),
                Cookie = new ConnectedAccountCookie
                {
                    AccountId = accountId,
                    EncryptedCookies = "encrypted-secret",
                    CookiesExpireDate = DateTime.UtcNow.AddDays(10)
                }
            };

            var userSetting = new UserSetting
            {
                Id = 1,
                UserId = userId,
                CompanyName = "Dentist Outreach Inc",
                CompanyPitch = "We provide dental leads"
            };

            await dbContext.Bots.AddAsync(bot);
            await dbContext.ConnectedAccounts.AddAsync(account);
            await dbContext.UserSettings.AddAsync(userSetting);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            SetupValidMocks(botId, "BOT_FACEBOOK");

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new CreateRunCommandHandler(
                dbContext, _encryptionServiceMock.Object, _networkClientMock.Object,
                _webhookResolverMock.Object, _userBotServiceMock.Object, _loggerMock.Object);

            var command = new CreateRunCommand(userId, new CreateRunDto(botId, accountId, null, "{}"));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            var run = await dbContext.Runs.FindAsync(result.Data);
            Assert.NotNull(run);
            Assert.Null(run.GroupId);
        }

        [Fact]
        public async Task Handle_WhenExternalCallReturnsFailure_ShouldCompensateAndReturnServerError()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;
            var accountId = 10;

            var bot = new Bot { Id = botId, Name = "Lead Bot", UiModuleCode = "BOT_FACEBOOK", CooldownMinutes = 10 };
            var account = new ConnectedAccount
            {
                Id = accountId,
                UserId = userId,
                BotId = botId,
                Bot = bot,
                Status = AccountStatus.ACTIVE.ToDbString(),
                Cookie = new ConnectedAccountCookie
                {
                    AccountId = accountId,
                    EncryptedCookies = "encrypted-secret",
                    CookiesExpireDate = DateTime.UtcNow.AddDays(10)
                }
            };

            var userSetting = new UserSetting
            {
                Id = 1,
                UserId = userId,
                CompanyName = "Dentist Outreach Inc",
                CompanyPitch = "We provide dental leads"
            };

            await dbContext.Bots.AddAsync(bot);
            await dbContext.ConnectedAccounts.AddAsync(account);
            await dbContext.UserSettings.AddAsync(userSetting);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            SetupValidMocks(botId, "BOT_FACEBOOK");

            // Network client returns failure
            _networkClientMock
                .Setup(n => n.PostJsonAsync(It.IsAny<string>(), It.IsAny<object>(), ExternalSystem.N8n, It.IsAny<CancellationToken>()))
                .ReturnsAsync(NetworkResult.Fail(500, "n8n server unavailable"));

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new CreateRunCommandHandler(
                dbContext, _encryptionServiceMock.Object, _networkClientMock.Object,
                _webhookResolverMock.Object, _userBotServiceMock.Object, _loggerMock.Object);

            var command = new CreateRunCommand(userId, new CreateRunDto(botId, accountId, null, "{}"));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.ServerError, result.ErrorType);
            Assert.Equal("Failed to dispatch run to external system.", result.Message);

            // Verify compensation: Run status is FAILED, Account status restored to ACTIVE
            var run = await dbContext.Runs.FirstOrDefaultAsync(r => r.UserId == userId);
            Assert.NotNull(run);
            Assert.Equal(RunStatus.FAILED.ToDbString(), run.Status);

            var updatedAccount = await dbContext.ConnectedAccounts.FindAsync(accountId);
            Assert.NotNull(updatedAccount);
            Assert.Equal(AccountStatus.ACTIVE.ToDbString(), updatedAccount.Status);
        }

        [Fact]
        public async Task Handle_WhenExternalCallThrowsException_ShouldCompensateAndReturnServerError()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var botId = 1;
            var accountId = 10;

            var bot = new Bot { Id = botId, Name = "Lead Bot", UiModuleCode = "BOT_FACEBOOK", CooldownMinutes = 10 };
            var account = new ConnectedAccount
            {
                Id = accountId,
                UserId = userId,
                BotId = botId,
                Bot = bot,
                Status = AccountStatus.ACTIVE.ToDbString(),
                Cookie = new ConnectedAccountCookie
                {
                    AccountId = accountId,
                    EncryptedCookies = "encrypted-secret",
                    CookiesExpireDate = DateTime.UtcNow.AddDays(10)
                }
            };

            var userSetting = new UserSetting
            {
                Id = 1,
                UserId = userId,
                CompanyName = "Dentist Outreach Inc",
                CompanyPitch = "We provide dental leads"
            };

            await dbContext.Bots.AddAsync(bot);
            await dbContext.ConnectedAccounts.AddAsync(account);
            await dbContext.UserSettings.AddAsync(userSetting);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            SetupValidMocks(botId, "BOT_FACEBOOK");

            // Network client throws exception (e.g. timeout / connection refused)
            _networkClientMock
                .Setup(n => n.PostJsonAsync(It.IsAny<string>(), It.IsAny<object>(), ExternalSystem.N8n, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new System.Net.Http.HttpRequestException("Connection refused"));

            _userBotServiceMock
                .Setup(s => s.CheckOwnershipAsync(userId, botId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new CreateRunCommandHandler(
                dbContext, _encryptionServiceMock.Object, _networkClientMock.Object,
                _webhookResolverMock.Object, _userBotServiceMock.Object, _loggerMock.Object);

            var command = new CreateRunCommand(userId, new CreateRunDto(botId, accountId, null, "{}"));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.ServerError, result.ErrorType);
            Assert.Equal("Failed to dispatch run to external system.", result.Message);

            // Verify compensation
            var run = await dbContext.Runs.FirstOrDefaultAsync(r => r.UserId == userId);
            Assert.NotNull(run);
            Assert.Equal(RunStatus.FAILED.ToDbString(), run.Status);

            var updatedAccount = await dbContext.ConnectedAccounts.FindAsync(accountId);
            Assert.NotNull(updatedAccount);
            Assert.Equal(AccountStatus.ACTIVE.ToDbString(), updatedAccount.Status);
        }
    }
}
