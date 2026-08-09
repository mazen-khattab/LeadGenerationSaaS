using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SaaS.Application.Common.Interfaces;
using SaaS.Domain.Entities;
using System.Security.Claims;

namespace SaaS.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestAuthController : ControllerBase
    {
        //    private readonly IEncryptionService _encryptionService;
        //    private readonly ITokenService _tokenService;

        //    public TestAuthController(
        //        IEncryptionService encryptionService,
        //        ITokenService tokenService)
        //    {
        //        _encryptionService = encryptionService;
        //        _tokenService = tokenService;
        //    }

        //    // 1. Test Encryption / Decryption Utility
        //    [HttpGet("test-encryption")]
        //    public IActionResult TestEncryption([FromQuery] string text = "MySecretFacebookCookieValue")
        //    {
        //        var encrypted = _encryptionService.Encrypt(text);
        //        var decrypted = _encryptionService.Decrypt(encrypted);

        //        return Ok(new
        //        {
        //            Original = text,
        //            Encrypted = encrypted,
        //            Decrypted = decrypted,
        //            IsSuccess = text == decrypted
        //        });
        //    }

        //    // 2. Simulate User Login (Issues Cookies & Sets SessionToken)
        //    [HttpPost("login-user")]
        //    public IActionResult LoginUser([FromQuery] Guid userId, [FromQuery] string sessionToken = "")
        //    {
        //        Console.WriteLine(Guid.NewGuid().ToString());
        //        var accessToken = _tokenService.GenerateAccessToken(
        //            userId: userId,
        //            email: "user@saas.com",
        //            sessionToken: sessionToken
        //        );

        //        var refreshToken = _tokenService.GenerateRefreshToken();

        //        // Set HttpOnly Cookies
        //        _tokenService.SetAuthCookies(accessToken, refreshToken);

        //        return Ok(new
        //        {
        //            Message = "User logged in successfully. Cookies set in response headers.",
        //            CurrentSessionToken = sessionToken,
        //            AccessToken = accessToken,
        //            RefeshToken = refreshToken,
        //        });
        //    }

        //    // 3. Simulate System Admin Login (No SessionToken needed)
        //    [HttpPost("login-admin")]
        //    public IActionResult LoginAdmin([FromQuery] Guid adminId)
        //    {
        //        var accessToken = _tokenService.GenerateAdminAccessToken(
        //            adminId: adminId,
        //            email: "admin@saas.com",
        //            role: "SystemAdmin"
        //        );

        //        var refreshToken = _tokenService.GenerateRefreshToken();

        //        _tokenService.SetAuthCookies(accessToken, refreshToken);

        //        return Ok(new
        //        {
        //            Message = "Admin logged in successfully.",
        //            AccessToken = accessToken,
        //            RefreshToken = refreshToken,
        //        });
        //    }

        //    // 4. Protected Endpoint (Requires Auth + Pass Through Middleware)
        //    [HttpGet("protected-data")]
        //    [Authorize]
        //    public IActionResult GetProtectedData()
        //    {
        //        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //        var role = User.FindFirstValue(ClaimTypes.Role);
        //        var sessionToken = User.FindFirstValue("SessionToken");

        //        return Ok(new
        //        {
        //            Message = "Access Granted! Middleware passed successfully.",
        //            UserId = userId,
        //            Role = role,
        //            SessionToken = sessionToken
        //        });
        //    }

        //    // 5. Logout Test
        //    [HttpPost("logout")]
        //    public IActionResult Logout()
        //    {
        //        _tokenService.ClearAuthCookies();
        //        return Ok(new { Message = "Cookies cleared successfully." });
        //    }
    }
}
