using System;
using Microsoft.AspNetCore.Mvc;

namespace SaaS.Api.Filters
{
    /// <summary>
    /// Attribute that uses TypeFilter to resolve the filter implementation which checks a configured webhook secret
    /// </summary>
    public class N8nWebhookAuthorizeAttribute : TypeFilterAttribute
    {
        public N8nWebhookAuthorizeAttribute() : base(typeof(N8nWebhookAuthorizeFilter)) { }
    }
}
