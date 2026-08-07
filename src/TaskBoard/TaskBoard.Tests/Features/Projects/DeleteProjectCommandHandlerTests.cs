using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Features.Projects.Commands;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;
using TaskBoard.Tests.Common;

namespace TaskBoard.Tests.Features.Projects
{
    public class DeleteProjectCommandHandlerTests : TestBase
    {
        private readonly DeleteProjectCommandHandler _handler;
        public DeleteProjectCommandHandlerTests() 
        {
            _handler = new DeleteProjectCommandHandler(
                MockContext.Object,
                MockCurrentUser.Object);
        }

        private Project CreateProjectWithOwner(Guid ownerId)
        {
            var project = new Project
            {
                Name = "Test Project",
                OwnerId = ownerId
            };

            var member = ProjectMember.Create(
                project.Id,
                ownerId,
                ProjectRole.Owner);

            project.Members.Add(member);
            project.Columns.Add(new TaskColumn
            {
                ProjectId = project.Id,
                Name = "To Do",
                Order = 1,
                Tasks = new List<TaskItem>()
            });

            return project;
        }

        [Fact]
        public async Task Handle_OwnerDeletes_ReturnsSuccess()
        {
            // Arrange
            var project = CreateProjectWithOwner(CurrentUserId);
            var projects = new List<Project> { project };

            MockContext.Setup(x => x.Projects)
                .Returns(projects.AsQueryable().BuildMockDbSet().Object);
            MockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new DeleteProjectCommand(project.Id);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_OwnerDeletes_SoftDeletesProject()
        {
            // Arrange
            var project = CreateProjectWithOwner(CurrentUserId);
            var projects = new List<Project> { project };

            MockContext.Setup(x => x.Projects)
                .Returns(projects.AsQueryable().BuildMockDbSet().Object);
            MockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new DeleteProjectCommand(project.Id);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert — project is soft deleted, not physically removed
            project.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_NonOwnerDeletes_ThrowsForbiddenException()
        {
            // Arrange — project owned by someone else
            var otherUserId = Guid.NewGuid();
            var project = CreateProjectWithOwner(otherUserId);
            var projects = new List<Project> { project };

            MockContext.Setup(x => x.Projects)
                .Returns(projects.AsQueryable().BuildMockDbSet().Object);

            var command = new DeleteProjectCommand(project.Id);

            // Act & Assert
            await FluentActions
                .Invoking(() => _handler.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task Handle_ProjectNotFound_ThrowsNotFoundException()
        {
            // Arrange — empty project list
            MockContext.Setup(x => x.Projects)
                .Returns(new List<Project>().AsQueryable().BuildMockDbSet().Object);

            var command = new DeleteProjectCommand(Guid.NewGuid());

            // Act & Assert
            await FluentActions
                .Invoking(() => _handler.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }
    }
}
