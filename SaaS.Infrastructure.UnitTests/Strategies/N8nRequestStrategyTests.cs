using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using SaaS.Application.Common.Settings;
using SaaS.Domain.Enums;
using SaaS.Infrastructure.Strategies;
using System.Net.Http.Headers;

namespace SaaS.Infrastructure.UnitTests.Strategies
{
    public class N8nRequestStrategyTests
    {
        private readonly Mock<IOptionsSnapshot<N8nOptions>> _mockOptions;
        private readonly N8nOptions _options;
        private readonly N8nRequestStrategy _strategy;

        public N8nRequestStrategyTests()
        {
            _mockOptions = new Mock<IOptionsSnapshot<N8nOptions>>();
            _options = new N8nOptions
            {
                BaseUrl = "https://n8n.service.internal:5678",
                N8nSecret = "secret-token-123"
            };
            _mockOptions.Setup(x => x.Value).Returns(_options);
            _strategy = new N8nRequestStrategy(_mockOptions.Object);
        }

        [Fact]
        public void System_ShouldReturnExternalSystemN8n()
        {
            // Act & Assert
            _strategy.System.Should().Be(ExternalSystem.N8n);
        }

        [Theory]
        [InlineData("https://n8n.cloud.com/webhook/test")]
        [InlineData("http://localhost:5678/webhook/456")]
        public void ResolveBaseUrl_ShouldReturnEmptyString_WhenEndpointIsAbsoluteUri(string absoluteEndpoint)
        {
            // Act
            var baseUrl = _strategy.ResolveBaseUrl(absoluteEndpoint);

            // Assert
            baseUrl.Should().BeEmpty();
        }

        [Theory]
        [InlineData("webhook/trigger-job")]
        [InlineData("/webhook/trigger-job")]
        [InlineData("relative-path")]
        public void ResolveBaseUrl_ShouldReturnConfiguredBaseUrl_WhenEndpointIsRelative(string relativeEndpoint)
        {
            // Act
            var baseUrl = _strategy.ResolveBaseUrl(relativeEndpoint);

            // Assert
            baseUrl.Should().Be(_options.BaseUrl);
        }

        [Fact]
        public void ApplyAuthentication_ShouldAddBearerToken_WhenN8nSecretIsConfigured()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Post, "https://n8n.service.internal/webhook/123");

            // Act
            _strategy.ApplyAuthentication(request);

            // Assert
            request.Headers.Authorization.Should().NotBeNull();
            request.Headers.Authorization!.Scheme.Should().Be("Bearer");
            request.Headers.Authorization.Parameter.Should().Be("secret-token-123");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ApplyAuthentication_ShouldNotAddAuthorizationHeader_WhenN8nSecretIsNullOrWhitespace(string? secret)
        {
            // Arrange
            _options.N8nSecret = secret!;
            var request = new HttpRequestMessage(HttpMethod.Post, "https://n8n.service.internal/webhook/123");

            // Act
            _strategy.ApplyAuthentication(request);

            // Assert
            request.Headers.Authorization.Should().BeNull();
        }
    }
}
