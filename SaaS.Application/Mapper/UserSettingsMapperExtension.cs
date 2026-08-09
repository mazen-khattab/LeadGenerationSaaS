using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Helpers;
using SaaS.Application.Common.Interfaces;
using SaaS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Mapper
{
    public static class UserSettingsMapperExtension
    {
        public static UserSettingsDto ToDto(this UserSetting settings, IEncryptionService encryptionService)
        {
            if (settings == null)
            {
                return new UserSettingsDto(false, string.Empty, false, string.Empty, 0);
            }

            var hasOpenAi = !string.IsNullOrWhiteSpace(settings.ScraperApiTokenEncrypted);
            var hasApify = !string.IsNullOrWhiteSpace(settings.AIApiKeyEncrypted);

            return new UserSettingsDto(
                HasAIApiKey: hasOpenAi,
                MaskedAIApiKey: Helper.MaskValue(settings.ScraperApiTokenEncrypted, encryptionService),
                HasScraperToken: hasApify,
                MaskedScraperToken: Helper.MaskValue(settings.AIApiKeyEncrypted, encryptionService),
                DailyLeadLimit: settings.DailyMessageLimit
            );
        }
    }
}
