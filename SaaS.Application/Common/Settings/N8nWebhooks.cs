using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Common.Settings
{
    public sealed class N8nWebhooks
    {
        public const string SectionName = "N8nWebhooks";
        public string FacebookMemberScraper { get; set; } = string.Empty;
        public string FacebookPostsScraper { get; set; } = string.Empty;
        public string InstagramScraper { get; set; } = string.Empty;
    }
}
