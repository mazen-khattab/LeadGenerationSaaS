using Microsoft.EntityFrameworkCore;
using SaaS.Application.Features.Worker.Commands.LogBotActivity;
using SaaS.Application.UnitTests.Common;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SaaS.Application.UnitTests.Features.Worker.Commands.LogBotActivity
{
    public class LogBotActivityCommandHandlerTests
    {
        private MockAppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<MockAppDbContext>()
                .UseInMemoryDatabase(databaseName: $"LogBotActivityDb_{Guid.NewGuid()}")
                .Options;

            return new MockAppDbContext(options);
        }

        [Fact]
        public void Constructor_WhenContextIsNull_ShouldThrowArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new LogBotActivityCommandHandler(null!));
            Assert.Equal("db", ex.ParamName);
        }

        [Fact]
        public async Task Handle_WhenCalled_ShouldPersistLogAndReturnSuccess()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var handler = new LogBotActivityCommandHandler(dbContext);

            var command = new LogBotActivityCommand
            {
                CorrelationId = "corr-abc-123",
                UserId = userId,
                LogLevel = "ERROR",
                Message = "Element not found during scraping",
                StackTrace = "at Scraper.Execute() in Program.cs:line 42"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Data);
            Assert.Equal("Bot activity logged successfully.", result.Message);

            var log = await dbContext.BotActivityLogs.FirstOrDefaultAsync(l => l.CorrelationId == "corr-abc-123");
            Assert.NotNull(log);
            Assert.Equal(userId, log.UserId);
            Assert.Equal("ERROR", log.LogLevel);
            Assert.Equal("Element not found during scraping", log.Message);
            Assert.Equal("at Scraper.Execute() in Program.cs:line 42", log.StackTrace);
            Assert.NotEqual(default, log.CreatedAt);
        }
    }
}
