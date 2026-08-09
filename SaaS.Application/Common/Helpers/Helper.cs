using SaaS.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Common.Helpers
{
    public static class Helper
    {
        public static string MaskValue(string? encryptedValue, IEncryptionService encryptionService)
        {
            if (string.IsNullOrWhiteSpace(encryptedValue))
                return string.Empty;

            try
            {
                var plain = encryptionService.Decrypt(encryptedValue) ?? string.Empty;
                if (plain.Length == 0)
                    return string.Empty;

                const int visible = 4;
                if (plain.Length <= visible)
                    return new string('*', plain.Length);

                // leave last `visible` characters visible, mask the rest using PadLeft
                return plain.Substring(plain.Length - visible).PadLeft(plain.Length, '*');
            }
            catch
            {
                // Don't leak exceptions or sensitive info
                return string.Empty;
            }
        }

    }
}
