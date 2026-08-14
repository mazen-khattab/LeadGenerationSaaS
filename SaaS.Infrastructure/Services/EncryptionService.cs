using Microsoft.Extensions.Options;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Settings;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SaaS.Infrastructure.Services
{
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        public EncryptionService(IOptionsMonitor<SecuritySettings> options)
        {
            var encryptionKey = options.CurrentValue.EncryptionKey;

            if (string.IsNullOrEmpty(encryptionKey))
            {
                throw new ArgumentException(nameof(encryptionKey), "EncryptionKey is missing in SecuritySettings configuration.");
            }

            _key = SHA256.HashData(Encoding.UTF8.GetBytes(encryptionKey));
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 Bytes (IV)
            var tag = new byte[AesGcm.TagByteSizes.MaxSize];     // 16 Bytes (Authentication Tag)
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = new byte[plainBytes.Length];

            RandomNumberGenerator.Fill(nonce);

            using var aesGcm = new AesGcm(_key, tag.Length);
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

            var result = new byte[nonce.Length + tag.Length + cipherBytes.Length];

            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, nonce.Length + tag.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            try
            {
                var fullCipher = Convert.FromBase64String(cipherText);

                var nonceSize = AesGcm.NonceByteSizes.MaxSize; // 12
                var tagSize = AesGcm.TagByteSizes.MaxSize;     // 16

                if (fullCipher.Length < nonceSize + tagSize)
                    throw new CryptographicException("Invalid ciphertext payload length.");

                var cipherSize = fullCipher.Length - nonceSize - tagSize;

                var nonce = fullCipher.AsSpan(0, nonceSize);
                var tag = fullCipher.AsSpan(nonceSize, tagSize);
                var cipherBytes = fullCipher.AsSpan(nonceSize + tagSize, cipherSize);
                var plainBytes = new byte[cipherSize];

                using var aesGcm = new AesGcm(_key, tagSize);

                aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("Failure to decrypt sensitive data. Data may have been tampered with or key is invalid.", ex);
            }
        }
    }
}
