using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Common.Settings
{
    public class GeneralSettings
    {
        public const string SectionName = "GeneralSettings";
        public int DailyLimitResetHour { get; set; } = 12;
        public int AccountCooldownperiodDays { get; set; } = 1;
    }
}
