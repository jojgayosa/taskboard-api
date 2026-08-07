using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Features.Members.Commands;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;
using TaskBoard.Tests.Common;

namespace TaskBoard.Tests.Features.Members
{

    public class AddMemberCommandHandlerTests : TestBase
    {
        private readonly AddMemberCommandHandler _handler;

        public AddMemberCommandHandlerTests()
        {
            _handler = new AddMemberCommandHandler(
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
                project.Id, ownerId, ProjectRole.Owner);
            project.Members.Add(member);

            return project;
        }

        [Fact]
        public async Task Handle_OwnerAddsNewMember_ReturnsSuccess()
        {
            // Arrange
            var project = CreateProjectWithOwner(CurrentUserId);
            var newUser = User.Create("newuser", "new@example.com", "hash");

            MockContext.Setup(x => x.Projects)
                .Returns(new List<Project> { project }.AsQueryable()
                    .BuildMockDbSet().Object);
            MockContext.Setup(x => x.Users)
                .Returns(new List<User> { newUser }.AsQueryable()
                    .BuildMockDbSet().Object);
            MockContext.Setup(x => x.ProjectMembers)
                .Returns(new List<ProjectMember>().AsQueryable()
                    .BuildMockDbSet().Object);
            MockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new AddMemberCommand(
                project.Id, newUser.Id, ProjectRole.Member);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data!.UserId.Should().Be(newUser.Id);
            result.Data.Role.Should().Be(ProjectRole.Member);
        }

        [Fact]
        public async Task Handle_NonOwnerAddsNewMember_ThrowsForbiddenException()
        {
            // Arrange — project owned by someone else
            var otherOwnerId = Guid.NewGuid();
            var project = CreateProjectWithOwner(otherOwnerId);

            // Current user is just a regular member
            var currentUserMember = ProjectMember.Create(
                project.Id, CurrentUserId, ProjectRole.Member);
            project.Members.Add(currentUserMember);

            MockContext.Setup(x => x.Projects)
                .Returns(new List<Project> { project }.AsQueryable()
                    .BuildMockDbSet().Object);

            var command = new AddMemberCommand(
                project.Id, Guid.NewGuid(), ProjectRole.Member);

            // Act & Assert
            await FluentActions
                .Invoking(() => _handler.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task Handle_UserAlreadyMember_ReturnsFailure()
        {
            // Arrange
            var project = CreateProjectWithOwner(CurrentUserId);
            var existingMemberUser = User.Create(
                "existing", "existing@example.com", "hash");

            // Add user as existing member
            var existingMember = ProjectMember.Create(
                project.Id, existingMemberUser.Id, ProjectRole.Member);
            project.Members.Add(existingMember);

            MockContext.Setup(x => x.Projects)
                .Returns(new List<Project> { project }.AsQueryable()
                    .BuildMockDbSet().Object);
            MockContext.Setup(x => x.Users)
                .Returns(new List<User> { existingMemberUser }.AsQueryable()
                    .BuildMockDbSet().Object);

            var command = new AddMemberCommand(
                project.Id, existingMemberUser.Id, ProjectRole.Member);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().Contain(
                "User is already a member of this project.");
        }

        [Fact]
        public async Task Handle_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var project = CreateProjectWithOwner(CurrentUserId);

            MockContext.Setup(x => x.Projects)
                .Returns(new List<Project> { project }.AsQueryable()
                    .BuildMockDbSet().Object);
            MockContext.Setup(x => x.Users)
                .Returns(new List<User>().AsQueryable()
                    .BuildMockDbSet().Object);

            var command = new AddMemberCommand(
                project.Id, Guid.NewGuid(), ProjectRole.Member);

            // Act & Assert
            await FluentActions
                .Invoking(() => _handler.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_ProjectNotFound_ThrowsNotFoundException()
        {
            // Arrange
            MockContext.Setup(x => x.Projects)
                .Returns(new List<Project>().AsQueryable()
                    .BuildMockDbSet().Object);

            var command = new AddMemberCommand(
                Guid.NewGuid(), Guid.NewGuid(), ProjectRole.Member);

            // Act & Assert
            await FluentActions
                .Invoking(() => _handler.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }
    }
}
