using BCrypt.Net;
using Microsoft.Extensions.Options;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Settings;

namespace SaaS.Infrastructure.Security
{
    public class PasswordHasher : IPasswordHasher
    {
        private readonly int _workFactor;

        public PasswordHasher(IOptions<SecuritySettings> securityOptions)
        {
            _workFactor = securityOptions.Value.PasswordWorkFactor;

            if (_workFactor is < 11 or > 15)
            {
                // 10 is roughly the floor for "still meaningfully slow" on modern hardware;
                // above 15 a single hash starts taking multiple seconds, which turns login
                // into a usability/DoS problem rather than a security improvement.
                throw new ArgumentOutOfRangeException(
                    nameof(securityOptions),
                    "PasswordWorkFactor must be between 11 and 15.");
            }
        }

        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            // BCrypt generates a secure, random salt per password and embeds both the
            // salt and the work factor into the returned hash string - nothing extra
            // needs to be stored alongside it.
            return BCrypt.Net.BCrypt.EnhancedHashPassword(password, _workFactor);
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
                return false;

            try
            {
                // EnhancedVerify uses SHA-384 pre-hashing so passwords longer than 72
                // bytes are verified correctly instead of being silently truncated.
                return BCrypt.Net.BCrypt.EnhancedVerify(password, passwordHash);
            }
            catch (SaltParseException)
            {
                // Stored hash isn't a valid bcrypt string (corrupted data, bad migration,
                // legacy format, etc.) - fail closed instead of throwing out of login.
                return false;
            }
        }

        public bool NeedsRehash(string passwordHash)
        {
            // Lets you detect passwords hashed under an older, lower work factor at
            // login time, so you can transparently re-hash them with HashPassword()
            // right after a successful VerifyPassword() - no forced password resets
            // needed when you raise the work factor later.
            try
            {
                return BCrypt.Net.BCrypt.PasswordNeedsRehash(passwordHash, _workFactor);
            }
            catch (SaltParseException)
            {
                return true; // unreadable hash - safest to treat as "needs rehash"
            }
        }
    }
}
