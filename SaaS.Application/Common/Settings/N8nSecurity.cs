using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Common.Settings
{
    public class N8nSecurity
    {
        public const string SectionName = "N8nSecurity";
        public string AuthSecret { get; set; } = string.Empty;
    }
}
