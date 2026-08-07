using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Features.Tasks.Commands;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;
using TaskBoard.Tests.Common;

namespace TaskBoard.Tests.Features.Tasks
{
    public class CreateTaskCommandHandlerTests : TestBase
    {
        private readonly CreateTaskCommandHandler _handler;
        public CreateTaskCommandHandlerTests()
        {
            _handler = new CreateTaskCommandHandler(
                MockContext.Object,
                MockCurrentUser.Object);
        }

        private TaskColumn CreateColumnWithProject(Guid currentUserId)
        {
            var project = new Project
            {
                Name = "Test Project",
                OwnerId = currentUserId
            };

            var member = ProjectMember.Create(project.Id, currentUserId, ProjectRole.Member);

            project.Members.Add(member);

            return new TaskColumn
            {
                ProjectId = project.Id,
                Name = "To Do",
                Order = 1,
                Project = project
            };
        }

        [Fact]
        public async Task Handle_ValidRequest_ReturnsSuccess()
        {
            //Arrange
            var column = CreateColumnWithProject(CurrentUserId);
            var columns = new List<TaskColumn> { column };
            var tasks = new List<TaskItem>();
            var activityLogs = new List<ActivityLog>();

            MockContext.Setup(x => x.TaskColumns).Returns(columns.AsQueryable().BuildMockDbSet().Object);
            MockContext.Setup(x => x.Tasks).Returns(tasks.AsQueryable().BuildMockDbSet().Object);
            MockContext.Setup(x => x.ActivityLogs).Returns(activityLogs.AsQueryable().BuildMockDbSet().Object);
            MockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var command = new CreateTaskCommand(
                column.Id,
                "Test Task",
                "Test Description",
                Priority.Medium,
                null, null);

            //Act
            var result = await _handler.Handle(command, CancellationToken.None);

            //Assert
            result.Succeeded.Should().BeTrue();
            result.Data!.Title.Should().Be("Test Task");
            result.Data.Priority.Should().Be(Priority.Medium);
            result.Data.ColumnId.Should().Be(column.Id);
        }

        [Fact]
        public async Task Handle_ColumnNotFound_ThrowsNotFoundException()
        {
            //Arrange
            var columns = new List<TaskColumn>();

            MockContext.Setup(x => x.TaskColumns).Returns(columns.AsQueryable().BuildMockDbSet().Object);

            var command = new CreateTaskCommand(
                Guid.NewGuid(),
                "Test Task",
                null,
                Priority.Medium,
                null, null);

            //Act & Assert
            await FluentActions
                .Invoking(() => _handler.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_NonMemberCreatesTask_ThrowsForbiddenException()
        {
            //Arrange
            var otherUserId = Guid.NewGuid();
            var column = CreateColumnWithProject(otherUserId);
            var columns = new List<TaskColumn> { column };

            MockContext.Setup(x => x.TaskColumns).Returns(columns.AsQueryable().BuildMockDbSet().Object);

            var command = new CreateTaskCommand(
                column.Id,
                "Test Task",
                null,
                Priority.Medium,
                null, null);

            //Act & Assert
            await FluentActions
                .Invoking(() => _handler.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task Handle_AssignedUserNotMember_ReturnsFailure()
        {
            //Arrange
            var column = CreateColumnWithProject(CurrentUserId);
            var columns = new List<TaskColumn> { column };

            MockContext.Setup(x => x.TaskColumns).Returns(columns.AsQueryable().BuildMockDbSet().Object);

            var otherUserId = Guid.NewGuid();

            var command = new CreateTaskCommand(
                column.Id,
                "Test Task",
                null,
                Priority.Medium,
                null,
                otherUserId);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeFalse();
            result.Errors.Should().Contain("Assigned user is not a member of this project.");
        }

        [Fact]
        public async Task Handle_ValidRequest_LogsActivity()
        {
            var column = CreateColumnWithProject(CurrentUserId);
            var columns = new List<TaskColumn> { column };
            var tasks = new List<TaskItem>();
            var activityLogs = new List<ActivityLog>();

            MockContext.Setup(x => x.TaskColumns).Returns(columns.AsQueryable().BuildMockDbSet().Object);
            MockContext.Setup(x => x.Tasks).Returns(tasks.AsQueryable().BuildMockDbSet().Object);
            MockContext.Setup(x => x.ActivityLogs).Returns(activityLogs.AsQueryable().BuildMockDbSet().Object);
            MockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var command = new CreateTaskCommand(
                column.Id, "Test Task", null, Priority.Medium, null, null);

            await _handler.Handle(command, CancellationToken.None);

            //Act & Assert
            MockContext.Verify(x =>
                x.ActivityLogs.Add(It.Is<ActivityLog>(a =>
                    a.CreatedBy == CurrentUserId &&
                    a.Action.Contains("Test Task"))),
                Times.Once);
        }
    }
}
