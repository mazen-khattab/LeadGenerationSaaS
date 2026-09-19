using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using SaaS.Application.Common.Settings;
using SaaS.Domain.Enums;
using SaaS.Infrastructure.Strategies;

namespace SaaS.Infrastructure.UnitTests.Strategies
{
    public class NodeWorkerRequestStrategyTests
    {
        private readonly Mock<IOptionsSnapshot<WorkerOptions>> _mockOptions;
        private readonly WorkerOptions _options;
        private readonly NodeWorkerRequestStrategy _strategy;

        public NodeWorkerRequestStrategyTests()
        {
            _mockOptions = new Mock<IOptionsSnapshot<WorkerOptions>>();
            _options = new WorkerOptions
            {
                BaseUrl = "http://worker.internal:4000",
                WorkerSecret = "super-secret-worker-key"
            };
            _mockOptions.Setup(x => x.Value).Returns(_options);
            _strategy = new NodeWorkerRequestStrategy(_mockOptions.Object);
        }

        [Fact]
        public void System_ShouldReturnExternalSystemNodeWorker()
        {
            // Act & Assert
            _strategy.System.Should().Be(ExternalSystem.NodeWorker);
        }

        [Theory]
        [InlineData("api/jobs")]
        [InlineData("/api/jobs")]
        [InlineData("http://anywhere/test")]
        public void ResolveBaseUrl_ShouldAlwaysReturnConfiguredBaseUrl(string endpoint)
        {
            // Act
            var baseUrl = _strategy.ResolveBaseUrl(endpoint);

            // Assert
            baseUrl.Should().Be(_options.BaseUrl);
        }

        [Fact]
        public void ApplyAuthentication_ShouldAddWorkerApiKeyHeader_WhenWorkerSecretIsConfigured()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Post, "http://worker.internal:4000/api/jobs");

            // Act
            _strategy.ApplyAuthentication(request);

            // Assert
            request.Headers.Contains("X-Worker-Api-Key").Should().BeTrue();
            request.Headers.GetValues("X-Worker-Api-Key").Should().ContainSingle()
                .Which.Should().Be("super-secret-worker-key");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ApplyAuthentication_ShouldNotAddWorkerApiKeyHeader_WhenWorkerSecretIsNullOrWhitespace(string? secret)
        {
            // Arrange
            _options.WorkerSecret = secret!;
            var request = new HttpRequestMessage(HttpMethod.Post, "http://worker.internal:4000/api/jobs");

            // Act
            _strategy.ApplyAuthentication(request);

            // Assert
            request.Headers.Contains("X-Worker-Api-Key").Should().BeFalse();
        }
    }
}
