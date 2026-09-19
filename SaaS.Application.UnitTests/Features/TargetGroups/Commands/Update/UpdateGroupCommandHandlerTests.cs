using Microsoft.EntityFrameworkCore;
using SaaS.Application.Common.Dtos;
using SaaS.Application.Common.Models;
using SaaS.Application.Features.TargetGroups.Commands.Update;
using SaaS.Application.UnitTests.Common;
using SaaS.Domain.Entities;
using SaaS.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SaaS.Application.UnitTests.Features.TargetGroups.Commands.Update
{
    public class UpdateGroupCommandHandlerTests
    {
        private MockAppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<MockAppDbContext>()
                .UseInMemoryDatabase(databaseName: $"UpdateGroupDb_{Guid.NewGuid()}")
                .Options;

            return new MockAppDbContext(options);
        }

        [Fact]
        public async Task Handle_WhenGroupNotFound_ShouldReturnNotFoundFailure()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var handler = new UpdateGroupCommandHandler(dbContext);
            var dto = new UpdateGroupDto("New Name", "https://example.com/new", "{}", true, null);
            var command = new UpdateGroupCommand(999, dto);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.NotFound, result.ErrorType);
            Assert.Equal("Target group not found", result.Message);
        }

        [Fact]
        public async Task Handle_WhenGroupExists_ShouldUpdatePropertiesAndReturnSuccess()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            var group = new TargetGroup
            {
                Id = 1,
                UserId = Guid.NewGuid(),
                BotId = 1,
                GroupName = "Old Name",
                GroupUrl = "https://example.com/old",
                ConfigJson = "{\"setting\": 1}",
                IsActive = true
            };

            await dbContext.TargetGroups.AddAsync(group);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new UpdateGroupCommandHandler(dbContext);
            var dto = new UpdateGroupDto("Updated Name", "https://example.com/updated", "{\"setting\": 2}", false, "cursor_123");
            var command = new UpdateGroupCommand(group.Id, dto);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Data);
            Assert.Equal("Target group updated successfully", result.Message);

            var updatedGroup = await dbContext.TargetGroups.FindAsync(group.Id);
            Assert.NotNull(updatedGroup);
            Assert.Equal("Updated Name", updatedGroup.GroupName);
            Assert.Equal("https://example.com/updated", updatedGroup.GroupUrl);
            Assert.False(updatedGroup.IsActive);
        }
    }
}
