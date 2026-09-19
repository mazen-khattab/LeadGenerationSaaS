using Microsoft.EntityFrameworkCore;
using SaaS.Application.Features.Users.Queries.GetUserById;
using SaaS.Application.UnitTests.Common;
using SaaS.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SaaS.Application.UnitTests.Features.Users.Queries.GetUserById
{
    public class GetUserByIdQueryHandlerTests
    {
        private MockAppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<MockAppDbContext>()
                .UseInMemoryDatabase(databaseName: $"GetUserByIdDb_{Guid.NewGuid()}")
                .Options;

            return new MockAppDbContext(options);
        }

        [Fact]
        public async Task Handle_WhenUserDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var handler = new GetUserByIdQueryHandler(dbContext);
            var query = new GetUserByIdQuery(Guid.NewGuid());

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_WhenUserIsDeleted_ShouldReturnNull()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Email = "deleted@example.com",
                PasswordHash = "hash",
                IsDeleted = true
            };

            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new GetUserByIdQueryHandler(dbContext);
            var query = new GetUserByIdQuery(userId);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_WhenUserExistsAndIsNotDeleted_ShouldReturnUser()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Email = "active@example.com",
                PasswordHash = "hash",
                IsDeleted = false
            };

            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new GetUserByIdQueryHandler(dbContext);
            var query = new GetUserByIdQuery(userId);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
            Assert.Equal("active@example.com", result.Email);
        }
    }
}
