using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using SaaS.Infrastructure.Services;

namespace SaaS.Infrastructure.UnitTests.Services
{
    public class N8nWebhookResolverTests
    {
        private readonly Mock<IOptionsMonitor<Dictionary<string, string>>> _mockOptions;

        public N8nWebhookResolverTests()
        {
            _mockOptions = new Mock<IOptionsMonitor<Dictionary<string, string>>>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GetWebhookUrl_ShouldThrowArgumentException_WhenBotCodeIsNullOrWhiteSpace(string? botCode)
        {
            // Arrange
            _mockOptions.Setup(x => x.CurrentValue).Returns(new Dictionary<string, string>());
            var resolver = new N8nWebhookResolver(_mockOptions.Object);

            // Act
            Action act = () => resolver.GetWebhookUrl(botCode!);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetWebhookUrl_ShouldThrowKeyNotFoundException_WhenBotCodeNotInConfiguration()
        {
            // Arrange
            _mockOptions.Setup(x => x.CurrentValue).Returns(new Dictionary<string, string>
            {
                ["TwitterScraper"] = "https://n8n.example.com/webhook/twitter"
            });
            var resolver = new N8nWebhookResolver(_mockOptions.Object);

            // Act
            Action act = () => resolver.GetWebhookUrl("FacebookScraper");

            // Assert
            act.Should().Throw<KeyNotFoundException>()
                .WithMessage("*Webhook URL for bot code 'FacebookScraper' was not found*");
        }

        [Fact]
        public void GetWebhookUrl_ShouldThrowKeyNotFoundException_WhenOptionsCurrentValueIsNull()
        {
            // Arrange
            _mockOptions.Setup(x => x.CurrentValue).Returns((Dictionary<string, string>?)null!);
            var resolver = new N8nWebhookResolver(_mockOptions.Object);

            // Act
            Action act = () => resolver.GetWebhookUrl("AnyBot");

            // Assert
            act.Should().Throw<KeyNotFoundException>()
                .WithMessage("*Webhook URL for bot code 'AnyBot' was not found*");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void GetWebhookUrl_ShouldThrowKeyNotFoundException_WhenWebhookUrlIsEmptyOrWhitespace(string emptyUrl)
        {
            // Arrange
            _mockOptions.Setup(x => x.CurrentValue).Returns(new Dictionary<string, string>
            {
                ["EmptyBot"] = emptyUrl
            });
            var resolver = new N8nWebhookResolver(_mockOptions.Object);

            // Act
            Action act = () => resolver.GetWebhookUrl("EmptyBot");

            // Assert
            act.Should().Throw<KeyNotFoundException>()
                .WithMessage("*Webhook URL for bot code 'EmptyBot' was not found*");
        }

        [Fact]
        public void GetWebhookUrl_ShouldReturnUrl_WhenBotCodeExists()
        {
            // Arrange
            var expectedUrl = "https://n8n.example.com/webhook/fb-lead-gen";
            _mockOptions.Setup(x => x.CurrentValue).Returns(new Dictionary<string, string>
            {
                ["FacebookScraper"] = expectedUrl
            });
            var resolver = new N8nWebhookResolver(_mockOptions.Object);

            // Act
            var result = resolver.GetWebhookUrl("FacebookScraper");

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Theory]
        [InlineData("facebookscraper")]
        [InlineData("FACEBOOKSCRAPER")]
        [InlineData("FaCeBoOkScRaPeR")]
        public void GetWebhookUrl_ShouldMatchCaseInsensitively(string botCodeLookup)
        {
            // Arrange
            var expectedUrl = "https://n8n.example.com/webhook/fb-lead-gen";
            _mockOptions.Setup(x => x.CurrentValue).Returns(new Dictionary<string, string>
            {
                ["FacebookScraper"] = expectedUrl
            });
            var resolver = new N8nWebhookResolver(_mockOptions.Object);

            // Act
            var result = resolver.GetWebhookUrl(botCodeLookup);

            // Assert
            result.Should().Be(expectedUrl);
        }
    }
}
