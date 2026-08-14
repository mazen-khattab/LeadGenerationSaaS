using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace SaaS.Api.Filters
{
    /// <summary>
    /// Attribute that uses TypeFilter to resolve the filter implementation which checks a configured Bot Worker API key
    /// </summary>
    public class BotWorkerAuthorizeAttribute : TypeFilterAttribute
    {
        public BotWorkerAuthorizeAttribute() : base(typeof(BotWorkerAuthorizeFilter)) { }
    }
}
