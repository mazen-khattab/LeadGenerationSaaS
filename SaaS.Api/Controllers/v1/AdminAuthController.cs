using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SaaS.Api.Extensions;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Models;
using SaaS.Application.Features.Auth.Commands.Admin;
using SaaS.Application.Features.Auth.Commands.Admin.Login;
using SaaS.Application.Features.Auth.Commands.Admin.Logout;
using SaaS.Application.Features.Auth.Commands.Admin.RefreshToken;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SaaS.Api.Controllers.v1
{
    [ApiController]
    [Route("api/v1/admin/auth")]
    public class AdminAuthController : ControllerBase
    {
        private readonly ISender _sender;

        public AdminAuthController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Admin login endpoint.
        /// Accepts email and password, sends AdminLoginCommand via MediatR.
        /// Returns admin DTO with status 200 OK.
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthLoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            var command = new AdminLoginCommand(request.Email, request.Password);
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
                Message = result?.Message ?? "Failure to Login.",
                Path = "api/v1/admin/auth/login",
                Method = HttpMethods.Post,
                TraceId = HttpContext.TraceIdentifier
            };

            return StatusCode(statusCode, errorResponse);
        }

        /// <summary>
        /// Admin refresh token endpoint.
        /// Reads refresh token from HTTP-Only cookie.
        /// Returns 401 Unauthorized if token is null.
        /// Sends AdminRefreshTokenCommand via MediatR.
        /// </summary>
        [HttpPost("refresh")]
        public async Task<ActionResult<AuthLoginResponseDto>> Refresh()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return Unauthorized();
            }

            var command = new AdminRefreshTokenCommand(refreshToken);
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
                Message = result?.Message ?? "Failure to Refresh tokens.",
                Path = "api/v1/admin/auth/refresh",
                Method = HttpMethods.Post,
                TraceId = HttpContext.TraceIdentifier
            };

            return StatusCode(statusCode, errorResponse);
        }

        /// <summary>
        /// Admin logout endpoint.
        /// Requires [Authorize] attribute.
        /// Extracts AdminId from claims (ClaimTypes.NameIdentifier).
        /// Reads refresh token from HTTP-Only cookie.
        /// Sends LogoutAdminCommand via MediatR.
        /// Cookie cleanup is handled inside command/token generator.
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var adminIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(adminIdString, out var adminId))
            {
                return BadRequest("Invalid admin ID in claims.");
            }

            var refreshToken = Request.Cookies["refreshToken"];

            var command = new LogoutAdminCommand(adminId, refreshToken);
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
                Message = result?.Message ?? "Failure to Logout.",
                Path = "api/v1/admin/auth/logout",
                Method = HttpMethods.Post,
                TraceId = HttpContext.TraceIdentifier
            };

            return StatusCode(statusCode, errorResponse);
        }
    }
}
