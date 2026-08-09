using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Domain.Entities
{
    public class ConnectedAccountCookie
    {
        public int AccountId { get; set; }
        public string EncryptedCookies { get; set; } = string.Empty;
        public DateTime CookiesExpireDate { get; set; }

        public ConnectedAccount Account { get; set; } = null!;
    }
}
