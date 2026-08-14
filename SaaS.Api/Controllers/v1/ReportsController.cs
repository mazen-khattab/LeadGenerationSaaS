using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Microsoft.OpenApi;

namespace SaaS.Api.Controllers.v1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider, ILogger<ReportsController> logger)
        {
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
            _logger = logger;
        }

        /// <summary>
        /// Very simple report listing controllers and their endpoints.
        /// Returns a plain-text report.
        /// </summary>
        [HttpGet("controllers")]
        public ActionResult GetControllersReport()
        {
            var items = _actionDescriptorCollectionProvider.ActionDescriptors.Items;

            var controllerActions = items
                .OfType<ControllerActionDescriptor>()
                .GroupBy(a => a.ControllerName)
                .OrderBy(g => g.Key);

            var sb = new StringBuilder();

            foreach (var group in controllerActions)
            {
                sb.AppendLine($"Controller: {group.Key}");

                foreach (var action in group.OrderBy(a => a.ActionName))
                {
                    // Try to get HTTP methods from action constraints; fall back to ANY when unknown
                    var httpMethods = action.ActionConstraints?
                        .OfType<HttpMethodActionConstraint>()
                        .SelectMany(c => c.HttpMethods)
                        .Distinct()
                        .ToArray() ?? new[] { "ANY" };

                    var template = action.AttributeRouteInfo?.Template ??
                                   (action.RouteValues != null && action.RouteValues.ContainsKey("action")
                                       ? $"api/v1/{action.ControllerName}/{action.ActionName}"
                                       : "<unknown route>");

                    sb.AppendLine($"  [{string.Join(", ", httpMethods)}] {template} -> {action.ActionName}");
                }

                sb.AppendLine();
            }

            return Content(sb.ToString(), "text/plain");
        }

        [HttpGet("dtos")]
        public ActionResult GetDtosReport()
        {
            var report = GetTypesReport(t =>
                (t.IsClass && !t.IsAbstract) &&
                ( (t.Namespace != null && t.Namespace.Contains(".Dtos")) || t.Name.EndsWith("Dto") ),
                "DTOs");

            return Content(report, "text/plain");
        }

        [HttpGet("commands")]
        public ActionResult GetCommandsReport()
        {
            var report = GetTypesReport(t =>
                (t.IsClass && !t.IsAbstract) &&
                ( (t.Namespace != null && t.Namespace.Contains(".Commands")) || t.Name.EndsWith("Command") ),
                "Commands");

            return Content(report, "text/plain");
        }

        [HttpGet("queries")]
        public ActionResult GetQueriesReport()
        {
            var report = GetTypesReport(t =>
                (t.IsClass && !t.IsAbstract) &&
                ( (t.Namespace != null && t.Namespace.Contains(".Queries")) || t.Name.EndsWith("Query") ),
                "Queries");

            return Content(report, "text/plain");
        }

        [HttpGet("services")]
        public ActionResult GetServicesReport()
        {
            var report = GetTypesReport(t =>
                t.IsClass && !t.IsAbstract && (
                    (t.Namespace != null && t.Namespace.Contains(".Services")) ||
                    t.Name.EndsWith("Service") ||
                    t.GetInterfaces().Any(i => i.Name.EndsWith("Service"))
                ),
                "Services");

            return Content(report, "text/plain");
        }


        [HttpGet("Entities")]
        public ActionResult GetEntitiesReport()
        {
            var report = GetTypesReport(t =>
                t.IsClass && !t.IsAbstract && (
                    (t.Namespace != null && t.Namespace.Contains(".Entities")) ||
                    t.Name.EndsWith("Entity")),
                "Entities");

            return Content(report, "text/plain");
        }

        private string GetTypesReport(System.Func<System.Type, bool> predicate, string title)
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && (a.GetName().Name?.StartsWith("SaaS") ?? false))
                .ToArray();

            var types = new List<System.Type>();

            foreach (var asm in assemblies)
            {
                try
                {
                    types.AddRange(asm.GetTypes());
                }
                catch (System.Reflection.ReflectionTypeLoadException ex)
                {
                    types.AddRange(ex.Types.Where(t => t != null)!);
                }
            }

            var matched = types
                .Where(t => t != null)
                .Where(predicate)
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine(title);
            sb.AppendLine(new string('-', 40));

            if (!matched.Any())
            {
                sb.AppendLine("(no items found)");
                return sb.ToString();
            }

            foreach (var g in matched.GroupBy(t => t.Namespace).OrderBy(g => g.Key))
            {
                sb.AppendLine($"Namespace: {g.Key}");
                foreach (var t in g)
                {
                    sb.AppendLine($"  {t.Name} ({t.FullName})");

                    if (g.Key == "SaaS.Domain.Entities")
                    {
                        foreach (var prop in t.GetProperties())
                        {
                            sb.AppendLine($"    Property: {prop.Name} ({prop.PropertyType.Name})");
                        }
                        sb.AppendLine();
                    }
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
