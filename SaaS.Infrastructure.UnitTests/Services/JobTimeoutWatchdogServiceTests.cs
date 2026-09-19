using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SaaS.Application.Common.Models;
using SaaS.Application.Common.Settings;
using SaaS.Application.Features.Worker.Commands.ProcessJobTimeouts;
using SaaS.Infrastructure.Services;

namespace SaaS.Infrastructure.UnitTests.Services
{
    public class JobTimeoutWatchdogServiceTests
    {
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IServiceScope> _mockScope;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<ILogger<JobTimeoutWatchdogService>> _mockLogger;
        private readonly JobWatchdogOptions _options;

        public JobTimeoutWatchdogServiceTests()
        {
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockScope = new Mock<IServiceScope>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockMediator = new Mock<IMediator>();
            _mockLogger = new Mock<ILogger<JobTimeoutWatchdogService>>();

            _options = new JobWatchdogOptions
            {
                CheckIntervalMinutes = 1,
                TimeoutThresholdMinutes = 15
            };

            _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);
            _mockScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(IMediator))).Returns(_mockMediator.Object);
        }

        private class TestableJobTimeoutWatchdogService : JobTimeoutWatchdogService
        {
            public TestableJobTimeoutWatchdogService(
                IServiceScopeFactory scopeFactory,
                ILogger<JobTimeoutWatchdogService> logger,
                IOptions<JobWatchdogOptions> options)
                : base(scopeFactory, logger, options)
            {
            }

            public Task RunExecuteAsync(CancellationToken stoppingToken) => ExecuteAsync(stoppingToken);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldNotExecute_WhenTokenIsAlreadyCancelled()
        {
            // Arrange
            var optionsWrapper = Options.Create(_options);
            var service = new TestableJobTimeoutWatchdogService(_mockScopeFactory.Object, _mockLogger.Object, optionsWrapper);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            await service.RunExecuteAsync(cts.Token);

            // Assert
            _mockScopeFactory.Verify(x => x.CreateScope(), Times.Never);
            _mockMediator.Verify(x => x.Send(It.IsAny<ProcessJobTimeoutsCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldDispatchProcessJobTimeoutsCommand_InScopedContainer()
        {
            // Arrange
            var optionsWrapper = Options.Create(_options);
            var service = new TestableJobTimeoutWatchdogService(_mockScopeFactory.Object, _mockLogger.Object, optionsWrapper);
            using var cts = new CancellationTokenSource();

            _mockMediator
                .Setup(x => x.Send(It.IsAny<ProcessJobTimeoutsCommand>(), It.IsAny<CancellationToken>()))
                .Callback(() => cts.Cancel())
                .ReturnsAsync(ApiResponse<bool>.Success(true));

            // Act
            Func<Task> act = async () => await service.RunExecuteAsync(cts.Token);

            // Assert - Delay will throw OperationCanceledException when token is cancelled
            await act.Should().ThrowAsync<OperationCanceledException>();
            _mockScopeFactory.Verify(x => x.CreateScope(), Times.Once);
            _mockMediator.Verify(x => x.Send(It.IsAny<ProcessJobTimeoutsCommand>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockScope.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldCatchExceptionAndLog_WhenMediatorThrows()
        {
            // Arrange
            var optionsWrapper = Options.Create(_options);
            var service = new TestableJobTimeoutWatchdogService(_mockScopeFactory.Object, _mockLogger.Object, optionsWrapper);
            using var cts = new CancellationTokenSource();

            _mockMediator
                .Setup(x => x.Send(It.IsAny<ProcessJobTimeoutsCommand>(), It.IsAny<CancellationToken>()))
                .Callback(() => cts.Cancel())
                .ThrowsAsync(new InvalidOperationException("Mediator failure"));

            // Act
            Func<Task> act = async () => await service.RunExecuteAsync(cts.Token);

            // Assert - Delay still throws OperationCanceledException, but the Mediator exception itself was caught
            await act.Should().ThrowAsync<OperationCanceledException>();
            _mockScopeFactory.Verify(x => x.CreateScope(), Times.Once);

            // Verify logger received error log
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error occurred executing JobTimeoutWatchdogService")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task StartAsyncAndStopAsync_ShouldStartAndStopCleanly()
        {
            // Arrange
            _options.CheckIntervalMinutes = 60;
            var optionsWrapper = Options.Create(_options);
            var service = new JobTimeoutWatchdogService(_mockScopeFactory.Object, _mockLogger.Object, optionsWrapper);

            var tcs = new TaskCompletionSource();
            _mockMediator
                .Setup(x => x.Send(It.IsAny<ProcessJobTimeoutsCommand>(), It.IsAny<CancellationToken>()))
                .Callback(() => tcs.TrySetResult())
                .ReturnsAsync(ApiResponse<bool>.Success(true));

            // Act
            await service.StartAsync(CancellationToken.None);
            await Task.WhenAny(tcs.Task, Task.Delay(2000));
            await service.StopAsync(CancellationToken.None);

            // Assert
            _mockMediator.Verify(x => x.Send(It.IsAny<ProcessJobTimeoutsCommand>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }
    }
}
