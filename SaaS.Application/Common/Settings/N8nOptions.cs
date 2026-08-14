using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Common.Settings
{
    public sealed class N8nOptions
    {
        public const string SectionName = "N8n";
        public string BaseUrl { get; set; } = string.Empty;
        public string N8nSecret { get; set; } = string.Empty;
    }
}
