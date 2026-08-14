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
        private readonly Dictionary<string, string> _n8nWebhooks;

        public N8nWebhookResolver(IOptionsMonitor<Dictionary<string, string>> options)
        {
            _n8nWebhooks = new Dictionary<string, string>
                (
                    options.CurrentValue ?? [],
                    StringComparer.OrdinalIgnoreCase
                );
        }

        public string GetWebhookUrl(string botCode)
        {
            if (string.IsNullOrWhiteSpace(botCode))
                return string.Empty;

            if (_n8nWebhooks.TryGetValue(botCode, out var url) && !string.IsNullOrWhiteSpace(url))
            {
                return url;
            }

            return string.Empty;
        }
    }
}
