using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace SaaS.Api.Controllers.v1
{
    [ApiController]
    [Route("api/v1/connected-accounts")]
    public class ConnectedAccountsController : ControllerBase
    {
        private readonly ISender _sender;

        public ConnectedAccountsController(ISender sender)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        }

        /// <summary>
        /// Create a new connected account.
        /// Maps the request to CreateConnectedAccountCommand and forwards to MediatR.
        /// Returns Ok(ApiResponse&lt;T&gt;) on success or a mapped ApiErrorResponse on failure.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<object>> Create([FromBody] object request)
        {
            var command = new CreateConnectedAccountCommand(request);
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

        /// <summary>
        /// Get all connected accounts.
        /// Maps to GetAllConnectedAccountsQuery and forwards to MediatR.
        /// Returns Ok(ApiResponse&lt;IEnumerable&lt;T&gt;&gt;) on success or a mapped ApiErrorResponse on failure.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<object>> GetAll()
        {
            var query = new GetAllConnectedAccountsQuery();
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

        /// <summary>
        /// Get connected account details by id.
        /// Maps to GetConnectedAccountByIdQuery and forwards to MediatR.
        /// Returns Ok(ApiResponse&lt;T&gt;) on success or a mapped ApiErrorResponse on failure.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetById([FromRoute] Guid id)
        {
            var query = new GetConnectedAccountByIdQuery(id);
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

        /// <summary>
        /// Update a connected account by id.
        /// Maps the request to UpdateConnectedAccountCommand and forwards to MediatR.
        /// Returns Ok(ApiResponse&lt;T&gt;) on success or a mapped ApiErrorResponse on failure.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<object>> Update([FromRoute] Guid id, [FromBody] object request)
        {
            var command = new UpdateConnectedAccountCommand(id, request);
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

        /// <summary>
        /// Delete a connected account by id.
        /// Maps to DeleteConnectedAccountCommand and forwards to MediatR.
        /// Returns Ok(ApiResponse&lt;T&gt;) on success or a mapped ApiErrorResponse on failure.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> Delete([FromRoute] Guid id)
        {
            var command = new DeleteConnectedAccountCommand(id);
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
