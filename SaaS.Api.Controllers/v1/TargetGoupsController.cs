using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Models;
using SaaS.Application.Features.TargetGroups.Commands.Add;
using SaaS.Application.Features.TargetGroups.Commands.Delete;
using SaaS.Application.Features.TargetGroups.Commands.Update;
using SaaS.Application.Features.TargetGroups.Queries.GetAll;
using SaaS.Application.Features.TargetGroups.Queries.GetById;
using System.Security.Claims;

namespace SaaS.Api.Controllers.v1
{
    [Route("api/v1/target-groups")]
    [Authorize]
    [ApiController]
    public class TargetGoupsController : ControllerBase
    {
        private readonly ISender _sender;

        public TargetGoupsController(ISender sender)
        {
            _sender = sender;
        }

        private Guid GetUserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : Guid.Empty;

        [HttpPost]
        public async Task<ActionResult<object>> Create([FromBody] AddGroupDto requestDto)
        {
            var userId = GetUserId();

            var command = new AddGroupCommand(userId, requestDto);
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

        [HttpGet]
        public async Task<ActionResult<object>> GetAll()
        {
            var userId = GetUserId();

            var query = new GetAllGroupsQuery(userId);
            var result = await _sender.Send(query);

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

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetById(int id)
        {
            var query = new GetGroupByIdQuery(id);
            var result = await _sender.Send(query);

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

        [HttpPut("{id}")]
        public async Task<ActionResult<object>> Update(int id, [FromBody] UpdateGroupDto requestDto)
        {
            var command = new UpdateGroupCommand(id, requestDto);
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

        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> Delete(int id)
        {
            var command = new DeleteGroupCommand(id);
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
