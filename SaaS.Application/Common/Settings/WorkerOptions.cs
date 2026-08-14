using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Common.Settings
{
    public sealed class WorkerOptions
    {
        public const string SectionName = "Worker";
        public string BaseUrl { get; set; } = string.Empty;
        public string WorkerSecret { get; set; } = string.Empty;
    }
}
