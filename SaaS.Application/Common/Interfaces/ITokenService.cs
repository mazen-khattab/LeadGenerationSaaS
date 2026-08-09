using SaaS.Application.Common.Models;
using SaaS.Domain.Entities;

namespace SaaS.Application.Common.Interfaces
{
    /// <summary>
    /// Interface for JWT token generation operations.
    /// Provides abstraction for creating access and refresh tokens for authenticated users.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Generates a short-lived access token for the specified user using the provided session token.
        /// </summary>
        /// <param name="userId">The authenticated user for whom the access token is created.</param>
        /// <param name="email">The email address used to include user identity information in the access token.</param>
        /// <param name="sessionToken">The current session token used to bind the generated access token to the session.</param>
        /// <returns>The generated access token string.</returns>
        string GenerateAccessToken(Guid userId, string email, string sessionToken);


        /// <summary>
        /// Generates a short-lived access token for the specified system administrator.
        /// </summary>
        /// <param name="adminId">The system administrator for whom the access token is created.</param>
        /// <param name="email">The email address used to include user identity information in the access token.</param>
        /// <param name="role">The role used to include authorization information in the access token.</param>
        /// <returns>The generated access token string.</returns>
        string GenerateAdminAccessToken(Guid adminId, string email, string role);


        /// <summary>
        /// Creates a long-lived refresh token associated with a user or an admin and stores it persistently.
        /// If both <paramref name="userId"/> and <paramref name="adminId"/> are provided, the token is associated with the admin by precedence (or adjust logic as needed).
        /// </summary>
        /// <returns>A task that resolves to the created refresh token string.</returns>
        string GenerateRefreshToken();


        /// <summary>
        /// Stores the access and refresh tokens in authentication cookies.
        /// </summary>
        /// <param name="accessToken">The access token to store in a cookie.</param>
        /// <param name="refreshToken">The refresh token to store in a cookie.</param>
        void SetAuthCookies(string accessToken, string refreshToken);


        /// <summary>
        /// Removes the authentication cookies from the current response.
        /// </summary>
        void ClearAuthCookies();


        /// <summary>
        /// Retrieves the access token from the current request cookies.
        /// </summary>
        /// <returns>The access token if present; otherwise, null.</returns>
        string? GetCookiesAccessToken();


        /// <summary>
        /// Retrieves the refresh token from the current request cookies.
        /// </summary>
        /// <returns>The refresh token if present; otherwise, null.</returns>
        string? GetCookiesRefreshToken();
    }
}
