using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using SaaS.Application.Common.Settings;
using SaaS.Infrastructure.Services;

namespace SaaS.Infrastructure.UnitTests.Services
{
    public class PasswordHasherServiceTests
    {
        private readonly Mock<IOptionsSnapshot<SecuritySettings>> _mockOptions;
        private readonly SecuritySettings _securitySettings;

        public PasswordHasherServiceTests()
        {
            _mockOptions = new Mock<IOptionsSnapshot<SecuritySettings>>();
            _securitySettings = new SecuritySettings
            {
                PasswordWorkFactor = 11 // Fast for unit tests, valid [11, 15]
            };
            _mockOptions.Setup(x => x.Value).Returns(_securitySettings);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(5)]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(16)]
        [InlineData(20)]
        public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenWorkFactorNotInRange(int workFactor)
        {
            // Arrange
            _securitySettings.PasswordWorkFactor = workFactor;

            // Act
            Action act = () => new PasswordHasherService(_mockOptions.Object);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithMessage("*PasswordWorkFactor must be between 11 and 15*");
        }

        [Theory]
        [InlineData(11)]
        [InlineData(12)]
        [InlineData(15)]
        public void Constructor_ShouldSucceed_WhenWorkFactorInRange(int workFactor)
        {
            // Arrange
            _securitySettings.PasswordWorkFactor = workFactor;

            // Act
            var service = new PasswordHasherService(_mockOptions.Object);

            // Assert
            service.Should().NotBeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void HashPassword_ShouldThrowArgumentException_WhenPasswordIsNullOrWhiteSpace(string? password)
        {
            // Arrange
            var service = new PasswordHasherService(_mockOptions.Object);

            // Act
            Action act = () => service.HashPassword(password!);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Password cannot be empty*");
        }

        [Fact]
        public void HashPassword_ShouldReturnValidBcryptHash_WhenPasswordIsValid()
        {
            // Arrange
            var service = new PasswordHasherService(_mockOptions.Object);
            var password = "SecureP@ssw0rd!123";

            // Act
            var hash = service.HashPassword(password);

            // Assert
            hash.Should().NotBeNullOrWhiteSpace();
            hash.Should().StartWith("$2");
        }

        [Fact]
        public void HashPassword_ShouldGenerateDifferentHashes_ForSamePassword_DueToUniqueSalts()
        {
            // Arrange
            var service = new PasswordHasherService(_mockOptions.Object);
            var password = "IdenticalPassword123!";

            // Act
            var hash1 = service.HashPassword(password);
            var hash2 = service.HashPassword(password);

            // Assert
            hash1.Should().NotBe(hash2);
        }

        [Theory]
        [InlineData(null, "$2a$11$something")]
        [InlineData("", "$2a$11$something")]
        [InlineData("   ", "$2a$11$something")]
        [InlineData("password", null)]
        [InlineData("password", "")]
        [InlineData("password", "   ")]
        public void VerifyPassword_ShouldReturnFalse_WhenInputIsNullOrWhiteSpace(string? password, string? hash)
        {
            // Arrange
            var service = new PasswordHasherService(_mockOptions.Object);

            // Act
            var result = service.VerifyPassword(password!, hash!);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void VerifyPassword_ShouldReturnTrue_WhenPasswordMatchesHash()
        {
            // Arrange
            var service = new PasswordHasherService(_mockOptions.Object);
            var password = "CorrectPassword123!";
            var hash = service.HashPassword(password);

            // Act
            var isMatch = service.VerifyPassword(password, hash);

            // Assert
            isMatch.Should().BeTrue();
        }

        [Fact]
        public void VerifyPassword_ShouldReturnFalse_WhenPasswordDoesNotMatchHash()
        {
            // Arrange
            var service = new PasswordHasherService(_mockOptions.Object);
            var correctPassword = "CorrectPassword123!";
            var wrongPassword = "WrongPassword456!";
            var hash = service.HashPassword(correctPassword);

            // Act
            var isMatch = service.VerifyPassword(wrongPassword, hash);

            // Assert
            isMatch.Should().BeFalse();
        }

        [Fact]
        public void VerifyPassword_ShouldReturnFalse_WhenHashIsMalformed()
        {
            // Arrange
            var service = new PasswordHasherService(_mockOptions.Object);
            var password = "SomePassword";
            var corruptHash = "not-a-valid-bcrypt-hash-string";

            // Act
            var isMatch = service.VerifyPassword(password, corruptHash);

            // Assert
            isMatch.Should().BeFalse();
        }

        [Fact]
        public void VerifyPassword_ShouldSupportLongPasswordsBeyond72Characters()
        {
            // Arrange
            var service = new PasswordHasherService(_mockOptions.Object);
            var longPassword = new string('A', 100);
            var hash = service.HashPassword(longPassword);

            // Act
            var isMatch = service.VerifyPassword(longPassword, hash);

            // Assert
            isMatch.Should().BeTrue();
        }

        [Fact]
        public void NeedsRehash_ShouldReturnFalse_WhenHashedWithCurrentWorkFactor()
        {
            // Arrange
            _securitySettings.PasswordWorkFactor = 11;
            var service = new PasswordHasherService(_mockOptions.Object);
            var hash = service.HashPassword("TestPassword");

            // Act
            var needsRehash = service.NeedsRehash(hash);

            // Assert
            needsRehash.Should().BeFalse();
        }

        [Fact]
        public void NeedsRehash_ShouldReturnTrue_WhenHashedWithLowerWorkFactor()
        {
            // Arrange: create hash with factor 11
            _securitySettings.PasswordWorkFactor = 11;
            var serviceFactor11 = new PasswordHasherService(_mockOptions.Object);
            var hash = serviceFactor11.HashPassword("TestPassword");

            // Change current settings to factor 12
            _securitySettings.PasswordWorkFactor = 12;
            var serviceFactor12 = new PasswordHasherService(_mockOptions.Object);

            // Act
            var needsRehash = serviceFactor12.NeedsRehash(hash);

            // Assert
            needsRehash.Should().BeTrue();
        }

        [Fact]
        public void NeedsRehash_ShouldReturnTrue_WhenHashIsMalformed()
        {
            // Arrange
            var service = new PasswordHasherService(_mockOptions.Object);
            var corruptHash = "malformed-hash";

            // Act
            var needsRehash = service.NeedsRehash(corruptHash);

            // Assert
            needsRehash.Should().BeTrue();
        }
    }
}
