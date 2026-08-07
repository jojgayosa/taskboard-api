using AutoMapper;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using TaskBoard.Application.Features.Projects.Commands;
using TaskBoard.Application.Features.Projects.DTOs;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;
using TaskBoard.Tests.Common;

namespace TaskBoard.Tests.Features.Projects
{
    public class CreateProjectCommandHandlerTests : TestBase
    {
        private readonly CreateProjectCommandHandler _handler;
        private readonly Mock<IMapper> _mockMapper;
        public CreateProjectCommandHandlerTests() 
        {
            _mockMapper = new Mock<IMapper>();

            _mockMapper.Setup(x => x.Map<ProjectDto>(It.IsAny<Project>()))
                .Returns((Project p) => new ProjectDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    OwnerId = p.OwnerId,
                    CreatedDate = p.CreatedDate
                });

            _handler = new CreateProjectCommandHandler(
                MockContext.Object,
                MockCurrentUser.Object,
                _mockMapper.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            var command = new CreateProjectCommand("Test Project", "Test Description");

            var projects = new List<Project>();
            var members = new List<ProjectMember>();

            MockContext.Setup(x => x.Projects)
                .Returns(projects.AsQueryable().BuildMockDbSet().Object);
            MockContext.Setup(x => x.ProjectMembers)
                .Returns(members.AsQueryable().BuildMockDbSet().Object);
            MockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("Test Project");
            result.Data.Description.Should().Be("Test Description");
            result.Data.OwnerId.Should().Be(CurrentUserId);
        }

        [Fact]
        public async Task Handle_ValidRequest_AddsOwnerAsMember()
        {
            // Arrange
            var command = new CreateProjectCommand("Test Project", null);

            var projects = new List<Project>();
            var members = new List<ProjectMember>();

            var mockMembersDbSet = members.AsQueryable().BuildMockDbSet();

            MockContext.Setup(x => x.Projects)
                .Returns(projects.AsQueryable().BuildMockDbSet().Object);
            MockContext.Setup(x => x.ProjectMembers)
                .Returns(mockMembersDbSet.Object);
            MockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert — owner was added as a project member
            MockContext.Verify(x =>
                x.ProjectMembers.Add(It.Is<ProjectMember>(m =>
                    m.UserId == CurrentUserId &&
                    m.Role == ProjectRole.Owner)),
                Times.Once);
        }

        [Fact]
        public async Task Handle_UnauthenticatedUser_ReturnsFailure()
        {
            // Arrange — simulate unauthenticated user
            MockCurrentUser.Setup(x => x.UserId).Returns((Guid?)null);

            var command = new CreateProjectCommand("Test Project", null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().Contain("User is not authenticated.");
        }

        [Fact]
        public async Task Handle_ValidRequest_SavesChanges()
        {
            // Arrange
            var command = new CreateProjectCommand("Test Project", null);

            MockContext.Setup(x => x.Projects)
                .Returns(new List<Project>().AsQueryable().BuildMockDbSet().Object);
            MockContext.Setup(x => x.ProjectMembers)
                .Returns(new List<ProjectMember>().AsQueryable().BuildMockDbSet().Object);
            MockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            MockContext.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
