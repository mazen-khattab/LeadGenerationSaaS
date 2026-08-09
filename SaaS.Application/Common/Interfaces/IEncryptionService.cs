using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Common.Interfaces
{
    public interface IEncryptionService
    {
        /// <summary>
        /// Encrypts a plaintext string into a Base64-encoded ciphertext using AES-256 with a unique random IV.
        /// </summary>
        /// <param name="plainText">The sensitive unencrypted text to encrypt.</param>
        /// <returns>A Base64 string containing the combined IV and encrypted data, or the original value if null/empty.</returns>
        string Encrypt(string plainText);

        /// <summary>
        /// Decrypts a Base64-encoded ciphertext back into its original plaintext string using AES-256.
        /// </summary>
        /// <param name="cipherText">The Base64 encrypted ciphertext payload containing the IV and cipher bytes.</param>
        /// <returns>The decrypted original plaintext string, or the original value if null/empty.</returns>
        string Decrypt(string cipherText);
    }
}
