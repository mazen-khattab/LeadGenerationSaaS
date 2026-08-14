using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Common.Interfaces
{
    public interface IPasswordHasherService
    {
        /// <summary>
        /// Generates a secure one-way cryptographic hash from a plaintext password using BCrypt with an automatic random salt.
        /// </summary>
        /// <param name="password">The plaintext password provided by the user.</param>
        /// <returns>A secure BCrypt hash string containing the algorithm parameters, salt, and hash value.</returns>
        string HashPassword(string password);

        /// <summary>
        /// Verifies whether a provided plaintext password matches a stored BCrypt password hash.
        /// </summary>
        /// <param name="password">The incoming plaintext password to verify.</param>
        /// <param name="passwordHash">The previously stored BCrypt password hash.</param>
        /// <returns><c>true</c> if the password matches the hash; otherwise, <c>false</c>.</returns>
        bool VerifyPassword(string password, string passwordHash);

        /// <summary>
        /// Checks if a stored password hash needs to be re-hashed to match the current security policy and WorkFactor settings.
        /// </summary>
        /// <param name="passwordHash">The existing stored password hash to inspect.</param>
        /// <returns><c>true</c> if the hash was created with an outdated WorkFactor or algorithm; otherwise, <c>false</c>.</returns>
        bool NeedsRehash(string passwordHash);
    }
}
