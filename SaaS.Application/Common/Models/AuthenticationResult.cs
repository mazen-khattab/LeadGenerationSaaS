namespace SaaS.Application.Common.Models
{
    public record AuthenticationResult
    {
        /// <summary>
        /// The JWT access token for authenticating API requests.
        /// </summary>
        public string AccessToken { get; init; } = string.Empty;

        /// <summary>
        /// The date and time when the access token expires.
        /// </summary>
        public DateTime AccessTokenExpiresAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// The refresh token used to obtain a new access token when the current one expires.
        /// </summary>
        public string RefreshToken { get; init; } = string.Empty;

        /// <summary>
        /// The date and time when the refresh token expires.
        /// </summary>
        public DateTime RefreshTokenExpiresAt { get; init; } = DateTime.UtcNow;
    }
}
