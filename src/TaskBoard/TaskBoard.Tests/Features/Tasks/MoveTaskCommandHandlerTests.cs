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
    public class MoveTaskCommandHandlerTests : TestBase
    {
        private readonly MoveTaskCommandHandler _handler;

        public MoveTaskCommandHandlerTests()
        {
            _handler = new MoveTaskCommandHandler(
                MockContext.Object,
                MockCurrentUser.Object);
        }

        private (TaskItem task, TaskColumn targetColumn) CreateTestData()
        {
            var project = new Project
            {
                Name = "Test Project",
                OwnerId = CurrentUserId
            };

            var member = ProjectMember.Create(
                project.Id, CurrentUserId, ProjectRole.Member);
            project.Members.Add(member);

            var sourceColumn = new TaskColumn
            {
                ProjectId = project.Id,
                Name = "To Do",
                Order = 1,
                Project = project
            };

            var targetColumn = new TaskColumn
            {
                ProjectId = project.Id,
                Name = "In Progress",
                Order = 2,
                Project = project
            };

            var task = new TaskItem
            {
                ColumnId = sourceColumn.Id,
                Title = "Test Task",
                Priority = Priority.Medium,
                Column = sourceColumn
            };

            return (task, targetColumn);
        }

        [Fact]
        public async Task Handle_ValidMove_ReturnsSuccess()
        {
            // Arrange
            var (task, targetColumn) = CreateTestData();

            MockContext.Setup(x => x.Tasks)
                .Returns(new List<TaskItem> { task }.AsQueryable()
                    .BuildMockDbSet().Object);
            MockContext.Setup(x => x.TaskColumns)
                .Returns(new List<TaskColumn> { targetColumn }.AsQueryable()
                    .BuildMockDbSet().Object);
            MockContext.Setup(x => x.ActivityLogs)
                .Returns(new List<ActivityLog>().AsQueryable()
                    .BuildMockDbSet().Object);
            MockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new MoveTaskCommand(task.Id, targetColumn.Id);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeTrue();
            task.ColumnId.Should().Be(targetColumn.Id);
        }

        [Fact]
        public async Task Handle_TaskNotFound_ThrowsNotFoundException()
        {
            // Arrange
            MockContext.Setup(x => x.Tasks)
                .Returns(new List<TaskItem>().AsQueryable()
                    .BuildMockDbSet().Object);

            var command = new MoveTaskCommand(Guid.NewGuid(), Guid.NewGuid());

            // Act & Assert
            await FluentActions
                .Invoking(() => _handler.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_TargetColumnInDifferentProject_ThrowsNotFoundException()
        {
            // Arrange
            var (task, _) = CreateTestData();

            // Target column belongs to a DIFFERENT project
            var differentProjectColumn = new TaskColumn
            {
                ProjectId = Guid.NewGuid(), // different project
                Name = "Other Column",
                Order = 1
            };

            MockContext.Setup(x => x.Tasks)
                .Returns(new List<TaskItem> { task }.AsQueryable()
                    .BuildMockDbSet().Object);
            MockContext.Setup(x => x.TaskColumns)
                .Returns(new List<TaskColumn> { differentProjectColumn }
                    .AsQueryable().BuildMockDbSet().Object);

            var command = new MoveTaskCommand(task.Id, differentProjectColumn.Id);

            // Act & Assert — should not allow moving to different project
            await FluentActions
                .Invoking(() => _handler.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_ValidMove_LogsActivity()
        {
            // Arrange
            var (task, targetColumn) = CreateTestData();
            var activityLogs = new List<ActivityLog>();

            MockContext.Setup(x => x.Tasks)
                .Returns(new List<TaskItem> { task }.AsQueryable()
                    .BuildMockDbSet().Object);
            MockContext.Setup(x => x.TaskColumns)
                .Returns(new List<TaskColumn> { targetColumn }.AsQueryable()
                    .BuildMockDbSet().Object);
            MockContext.Setup(x => x.ActivityLogs)
                .Returns(activityLogs.AsQueryable().BuildMockDbSet().Object);
            MockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new MoveTaskCommand(task.Id, targetColumn.Id);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert — move was logged
            MockContext.Verify(x =>
                x.ActivityLogs.Add(It.Is<ActivityLog>(a =>
                    a.CreatedBy == CurrentUserId &&
                    a.Action.Contains("moved"))),
                Times.Once);
        }
    }
}
