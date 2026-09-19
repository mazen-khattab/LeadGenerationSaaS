using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaaS.Application.Common.Interfaces;
using SaaS.Domain.Enums;
using SaaS.Infrastructure.Services;
using System.Net;
using System.Text;

namespace SaaS.Infrastructure.UnitTests.Services
{
    public class NetworkClientTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<IExternalSystemRequestStrategy> _mockN8nStrategy;
        private readonly Mock<IExternalSystemRequestStrategy> _mockNodeWorkerStrategy;
        private readonly Mock<ILogger<NetworkClient>> _mockLogger;
        private readonly TestHttpMessageHandler _httpHandler;
        private readonly HttpClient _httpClient;

        public NetworkClientTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockN8nStrategy = new Mock<IExternalSystemRequestStrategy>();
            _mockNodeWorkerStrategy = new Mock<IExternalSystemRequestStrategy>();
            _mockLogger = new Mock<ILogger<NetworkClient>>();

            _mockN8nStrategy.Setup(x => x.System).Returns(ExternalSystem.N8n);
            _mockNodeWorkerStrategy.Setup(x => x.System).Returns(ExternalSystem.NodeWorker);

            _httpHandler = new TestHttpMessageHandler();
            _httpClient = new HttpClient(_httpHandler);

            _mockHttpClientFactory
                .Setup(x => x.CreateClient(NetworkClient.NamedClient))
                .Returns(_httpClient);
        }

        private NetworkClient CreateClient()
        {
            return new NetworkClient(
                _mockHttpClientFactory.Object,
                new[] { _mockN8nStrategy.Object, _mockNodeWorkerStrategy.Object },
                _mockLogger.Object);
        }

        private class TestHttpMessageHandler : HttpMessageHandler
        {
            public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Handler { get; set; }
                = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"status\":\"ok\"}", Encoding.UTF8, "application/json")
                });

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Handler(request, cancellationToken);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task PostJsonAsync_ShouldReturnFail_WhenEndpointIsNullOrWhitespace(string? endpoint)
        {
            // Arrange
            var client = CreateClient();

            // Act
            var result = await client.PostJsonAsync(endpoint!, new { Data = 123 }, ExternalSystem.N8n);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Endpoint cannot be null or empty.");
        }

        [Fact]
        public async Task PostJsonAsync_ShouldReturnFail_WhenPayloadIsNull()
        {
            // Arrange
            var client = CreateClient();

            // Act
            var result = await client.PostJsonAsync("webhook/test", null!, ExternalSystem.N8n);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Payload cannot be null.");
        }

        [Fact]
        public async Task PostJsonAsync_ShouldThrowNotSupportedException_WhenStrategyNotFound()
        {
            // Arrange
            var client = new NetworkClient(
                _mockHttpClientFactory.Object,
                Array.Empty<IExternalSystemRequestStrategy>(),
                _mockLogger.Object);

            // Act
            Func<Task> act = async () => await client.PostJsonAsync("endpoint", new { }, ExternalSystem.N8n);

            // Assert
            await act.Should().ThrowAsync<NotSupportedException>()
                .WithMessage("*External system 'N8n' has no registered request startegy*");
        }

        [Fact]
        public async Task PostJsonAsync_ShouldExecutePost_AndReturnOk_WhenRequestSucceeds()
        {
            // Arrange
            _mockN8nStrategy.Setup(x => x.ResolveBaseUrl("webhook/test")).Returns("https://n8n.example.com");
            _mockN8nStrategy.Setup(x => x.ApplyAuthentication(It.IsAny<HttpRequestMessage>()));

            HttpRequestMessage? capturedRequest = null;
            _httpHandler.Handler = (req, _) =>
            {
                capturedRequest = req;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"result\":\"success\"}", Encoding.UTF8, "application/json")
                });
            };

            var client = CreateClient();

            // Act
            var result = await client.PostJsonAsync("webhook/test", new { JobId = 42 }, ExternalSystem.N8n);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Content.Should().Be("{\"result\":\"success\"}");

            capturedRequest.Should().NotBeNull();
            capturedRequest!.Method.Should().Be(HttpMethod.Post);
            capturedRequest.RequestUri.Should().Be(new Uri("https://n8n.example.com/webhook/test"));

            _mockN8nStrategy.Verify(x => x.ApplyAuthentication(It.IsAny<HttpRequestMessage>()), Times.Once);
        }

        [Fact]
        public async Task PostJsonAsync_ShouldHandleAbsoluteUri_WhenBaseUrlIsEmpty()
        {
            // Arrange
            _mockN8nStrategy.Setup(x => x.ResolveBaseUrl("https://direct.webhook.com/path")).Returns(string.Empty);
            _mockN8nStrategy.Setup(x => x.ApplyAuthentication(It.IsAny<HttpRequestMessage>()));

            HttpRequestMessage? capturedRequest = null;
            _httpHandler.Handler = (req, _) =>
            {
                capturedRequest = req;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("OK")
                });
            };

            var client = CreateClient();

            // Act
            var result = await client.PostJsonAsync("https://direct.webhook.com/path", new { }, ExternalSystem.N8n);

            // Assert
            result.IsSuccess.Should().BeTrue();
            capturedRequest!.RequestUri.Should().Be(new Uri("https://direct.webhook.com/path"));
        }

        [Fact]
        public async Task PostJsonAsync_ShouldReturnFail_WhenResponseIsNonSuccess()
        {
            // Arrange
            _mockN8nStrategy.Setup(x => x.ResolveBaseUrl(It.IsAny<string>())).Returns("https://n8n.example.com");
            _httpHandler.Handler = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":\"Invalid payload\"}")
            });

            var client = CreateClient();

            // Act
            var result = await client.PostJsonAsync("webhook/test", new { Data = "bad" }, ExternalSystem.N8n);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.ErrorMessage.Should().Be("{\"error\":\"Invalid payload\"}");
        }

        [Fact]
        public async Task PostJsonAsync_ShouldReturnTimeoutFail_WhenTaskCanceledExceptionOccurs()
        {
            // Arrange
            _mockN8nStrategy.Setup(x => x.ResolveBaseUrl("webhook/test")).Returns("https://n8n.example.com");
            _httpHandler.Handler = (_, _) => throw new TaskCanceledException("HttpClient timeout");

            var client = CreateClient();

            // Act
            var result = await client.PostJsonAsync("webhook/test", new { }, ExternalSystem.N8n, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().BeNull();
            result.ErrorMessage.Should().Be("Request timed out.");
        }

        [Fact]
        public async Task PostJsonAsync_ShouldReturnNetworkErrorFail_WhenHttpRequestExceptionOccurs()
        {
            // Arrange
            _mockN8nStrategy.Setup(x => x.ResolveBaseUrl("webhook/test")).Returns("https://n8n.example.com");
            _httpHandler.Handler = (_, _) => throw new HttpRequestException("Connection refused");

            var client = CreateClient();

            // Act
            var result = await client.PostJsonAsync("webhook/test", new { }, ExternalSystem.N8n);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().BeNull();
            result.ErrorMessage.Should().Be("Network error occurred.");
        }

        [Fact]
        public async Task GetAsync_ShouldExecuteGet_AndReturnOk_WhenRequestSucceeds()
        {
            // Arrange
            _mockNodeWorkerStrategy.Setup(x => x.ResolveBaseUrl("health")).Returns("http://worker.local:3000");
            HttpRequestMessage? capturedRequest = null;
            _httpHandler.Handler = (req, _) =>
            {
                capturedRequest = req;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"status\":\"healthy\"}")
                });
            };

            var client = CreateClient();

            // Act
            var result = await client.GetAsync("health", ExternalSystem.NodeWorker);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Content.Should().Be("{\"status\":\"healthy\"}");
            capturedRequest!.Method.Should().Be(HttpMethod.Get);
            capturedRequest.RequestUri.Should().Be(new Uri("http://worker.local:3000/health"));
        }

        [Fact]
        public async Task GetAsync_ShouldReturnFail_WhenResponseIsNotFound()
        {
            // Arrange
            _mockNodeWorkerStrategy.Setup(x => x.ResolveBaseUrl("jobs/999")).Returns("http://worker.local:3000");
            _httpHandler.Handler = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("Job not found")
            });

            var client = CreateClient();

            // Act
            var result = await client.GetAsync("jobs/999", ExternalSystem.NodeWorker);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.ErrorMessage.Should().Be("Job not found");
        }
    }
}
