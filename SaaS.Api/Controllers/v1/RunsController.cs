using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Models;
using SaaS.Api.Extensions;
using SaaS.Application.Features.Runs.Commands.Create;
using SaaS.Application.Features.Runs.Commands.Complete;
using SaaS.Api.Filters;

namespace SaaS.Api.Controllers.v1
{
    [ApiController]
    [Route("api/v1/runs")]
    public class RunsController : ControllerBase
    {
        private readonly ISender _sender;

        public RunsController(ISender sender)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        }

        /// <summary>
        /// Called by n8n when a run completes scraping.
        /// Protected by X-Webhook-Secret header validated by N8nWebhookAuthorizeFilter.
        /// </summary>
        [HttpPost("{id}/complete")]
        [N8nWebhookAuthorize]
        public async Task<ActionResult<object>> Complete(int id, [FromBody] CompleteRunDto request)
        {
            var command = new CompleteRunCommand(id, request?.ExtractedLeads ?? []);
            var result = await _sender.Send(command);

            if (result is not null && result.IsSuccess)
            {
                return Ok(result);
            }

            int statusCode = result!.ErrorType.ToHttpStatusCode();

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = statusCode,
                Message = result?.Message ?? "Failure during operation.",
                Path = HttpContext.Request.Path,
                Method = HttpContext.Request.Method,
                TraceId = HttpContext.TraceIdentifier
            };

            return StatusCode(statusCode, errorResponse);
        }

        private Guid GetUserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : Guid.Empty;

        [HttpPost]
        public async Task<ActionResult<object>> Create([FromBody] CreateRunDto requestDto)
        {
            var userId = GetUserId();
            var command = new CreateRunCommand(userId, requestDto);
            var result = await _sender.Send(command);

            if (result is not null && result.IsSuccess)
            {
                return Ok(result);
            }

            int statusCode = result!.ErrorType.ToHttpStatusCode();

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = statusCode,
                Message = result?.Message ?? "Failure during operation.",
                Path = HttpContext.Request.Path,
                Method = HttpContext.Request.Method,
                TraceId = HttpContext.TraceIdentifier
            };

            return StatusCode(statusCode, errorResponse);
        }
    }
}
