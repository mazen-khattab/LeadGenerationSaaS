using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Settings;

namespace SaaS.Infrastructure.Services
{
    /// <summary>
    /// Infrastructure implementation that resolves n8n webhook URLs from configuration.
    /// Expects configuration section: "N8nWebhooks" with keys per bot code.
    /// Example: "N8nWebhooks:FacebookScraper" = "https://.../webhook/..."
    /// </summary>
    public class N8nWebhookResolver : IN8nWebhookResolver
    {
        private readonly IOptionsMonitor<Dictionary<string, string>> _options;

        public N8nWebhookResolver(IOptionsMonitor<Dictionary<string, string>> options)
        {
            _options = options;
        }

        public string GetWebhookUrl(string botCode)
        {
            Dictionary<string, string> n8nWebhooks = new(
                    _options.CurrentValue ?? [],
                    StringComparer.OrdinalIgnoreCase
                );

            ArgumentException.ThrowIfNullOrWhiteSpace(botCode, nameof(botCode));

            if (!n8nWebhooks.TryGetValue(botCode, out var url) || string.IsNullOrWhiteSpace(url))
            {
                throw new KeyNotFoundException($"Webhook URL for bot code '{botCode}' was not found in configuration.");
            }

            return url;
        }
    }
}
