using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using SaaS.Application.Common.Settings;
using SaaS.Infrastructure.Services;
using System.Security.Cryptography;
using System.Text;

namespace SaaS.Infrastructure.UnitTests.Services
{
    public class EncryptionServiceTests
    {
        private readonly Mock<IOptionsMonitor<SecuritySettings>> _mockOptions;
        private readonly SecuritySettings _securitySettings;

        public EncryptionServiceTests()
        {
            _mockOptions = new Mock<IOptionsMonitor<SecuritySettings>>();
            _securitySettings = new SecuritySettings
            {
                EncryptionKey = "MySuperSecretKeyForAesGcmTesting!"
            };
            _mockOptions.Setup(x => x.CurrentValue).Returns(_securitySettings);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Encrypt_ShouldReturnOriginalInput_WhenPlainTextIsNullOrEmpty(string? input)
        {
            // Arrange
            var service = new EncryptionService(_mockOptions.Object);

            // Act
            var result = service.Encrypt(input!);

            // Assert
            result.Should().Be(input);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Encrypt_ShouldThrowArgumentException_WhenEncryptionKeyIsNullOrEmpty(string? key)
        {
            // Arrange
            _securitySettings.EncryptionKey = key!;
            var service = new EncryptionService(_mockOptions.Object);

            // Act
            Action act = () => service.Encrypt("hello world");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*EncryptionKey is missing*");
        }

        [Fact]
        public void Encrypt_ShouldReturnBase64String_WhenPlainTextIsValid()
        {
            // Arrange
            var service = new EncryptionService(_mockOptions.Object);
            var plainText = "SecretPassword123!";

            // Act
            var cipherText = service.Encrypt(plainText);

            // Assert
            cipherText.Should().NotBeNullOrWhiteSpace();
            var act = () => Convert.FromBase64String(cipherText);
            act.Should().NotThrow();
        }

        [Fact]
        public void Encrypt_ShouldProduceDifferentCipherTexts_ForSamePlainText_DueToRandomNonce()
        {
            // Arrange
            var service = new EncryptionService(_mockOptions.Object);
            var plainText = "DeterministicInput";

            // Act
            var cipher1 = service.Encrypt(plainText);
            var cipher2 = service.Encrypt(plainText);

            // Assert
            cipher1.Should().NotBe(cipher2);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Decrypt_ShouldReturnOriginalInput_WhenCipherTextIsNullOrEmpty(string? input)
        {
            // Arrange
            var service = new EncryptionService(_mockOptions.Object);

            // Act
            var result = service.Decrypt(input!);

            // Assert
            result.Should().Be(input);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Decrypt_ShouldThrowArgumentException_WhenEncryptionKeyIsNullOrEmpty(string? key)
        {
            // Arrange
            _securitySettings.EncryptionKey = key!;
            var service = new EncryptionService(_mockOptions.Object);

            // Act
            Action act = () => service.Decrypt("c29tZWNpcGhlcnRleHQ=");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*EncryptionKey is missing*");
        }

        [Theory]
        [InlineData("Hello, World!")]
        [InlineData("A")]
        [InlineData("Special characters: !@#$%^&*()_+{}|:\"<>?~`-=[]\\;',./")]
        [InlineData("Unicode: 🚀🌟 مرحبا بكم \u263A")]
        [InlineData("Very long payload: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.")]
        public void EncryptAndDecrypt_ShouldSuccessfullyRoundTrip(string plainText)
        {
            // Arrange
            var service = new EncryptionService(_mockOptions.Object);

            // Act
            var encrypted = service.Encrypt(plainText);
            var decrypted = service.Decrypt(encrypted);

            // Assert
            decrypted.Should().Be(plainText);
        }

        [Fact]
        public void Decrypt_ShouldThrowInvalidOperationException_WhenCipherPayloadIsShorterThanNonceAndTag()
        {
            // Arrange (less than 12 + 16 = 28 bytes)
            var service = new EncryptionService(_mockOptions.Object);
            var shortPayload = Convert.ToBase64String(new byte[20]);

            // Act
            Action act = () => service.Decrypt(shortPayload);

            // Assert
            var ex = act.Should().Throw<InvalidOperationException>().Which;
            ex.Message.Should().Contain("Failure to decrypt sensitive data");
            ex.InnerException.Should().BeAssignableTo<CryptographicException>();
        }

        [Fact]
        public void Decrypt_ShouldThrowInvalidOperationException_WhenCipherPayloadIsTampered()
        {
            // Arrange
            var service = new EncryptionService(_mockOptions.Object);
            var encrypted = service.Encrypt("SensitiveData");
            var rawBytes = Convert.FromBase64String(encrypted);

            // Tamper with the last byte
            rawBytes[^1] ^= 0xFF;
            var tamperedCipher = Convert.ToBase64String(rawBytes);

            // Act
            Action act = () => service.Decrypt(tamperedCipher);

            // Assert
            var ex = act.Should().Throw<InvalidOperationException>().Which;
            ex.Message.Should().Contain("Failure to decrypt sensitive data");
            ex.InnerException.Should().BeAssignableTo<CryptographicException>();
        }

        [Fact]
        public void Decrypt_ShouldThrowInvalidOperationException_WhenDecryptedWithDifferentKey()
        {
            // Arrange
            var service1 = new EncryptionService(_mockOptions.Object);
            var encrypted = service1.Encrypt("SensitiveData");

            var mockDifferentOptions = new Mock<IOptionsMonitor<SecuritySettings>>();
            mockDifferentOptions.Setup(x => x.CurrentValue).Returns(new SecuritySettings
            {
                EncryptionKey = "CompletelyDifferentEncryptionKey!"
            });
            var service2 = new EncryptionService(mockDifferentOptions.Object);

            // Act
            Action act = () => service2.Decrypt(encrypted);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithInnerException<CryptographicException>();
        }
    }
}
