using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using TaskBoard.Application.Features.Auth.Commands;
using TaskBoard.Domain.Entities;
using TaskBoard.Tests.Common;

namespace TaskBoard.Tests.Features.Auth
{
    public class RegisterCommandHandlerTests : TestBase
    {
        private readonly RegisterCommandHandler _handler;

        public RegisterCommandHandlerTests() 
        {
            _handler = new RegisterCommandHandler(
                MockContext.Object,
                MockPasswordHasher.Object,
                MockTokenService.Object);

        }

        [Fact]
        public async Task Handle_ValidRequest_ReturnSuccess()
        {
            //Arrange
            var command = new RegisterCommand(
            "testuser",
            "test@example.com",
            "Test@1234");

            // Empty user list — no existing users
            var users = new List<User>();
            var mockDbSet = users.AsQueryable().BuildMockDbSet();

            MockContext.Setup(x => x.Users).Returns(mockDbSet.Object);
            MockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Username.Should().Be("testuser");
            result.Data.Email.Should().Be("test@example.com");
            result.Data.AccessToken.Should().Be("fake_access_token");
            result.Data.RefreshToken.Should().Be("fake_refresh_token");
        }

        [Fact]
        public async Task Handle_DuplicateEmail_ReturnsFailure()
        {
            // Arrange
            var command = new RegisterCommand(
                "newuser",
                "existing@example.com",
                "Test@1234");

            // Existing user with same email
            var existingUser = User.Create(
                "existinguser",
                "existing@example.com",
                "hashed_password");

            var users = new List<User> { existingUser };
            var mockDbSet = users.AsQueryable().BuildMockDbSet();

            MockContext.Setup(x => x.Users).Returns(mockDbSet.Object);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().Contain("Email is already registered.");
        }

        [Fact]
        public async Task Handle_DuplicateUsername_ReturnsFailure()
        {
            // Arrange
            var command = new RegisterCommand(
                "existinguser",
                "new@example.com",
                "Test@1234");

            var existingUser = User.Create(
                "existinguser",
                "existing@example.com",
                "hashed_password");

            var users = new List<User> { existingUser };
            var mockDbSet = users.AsQueryable().BuildMockDbSet();

            MockContext.Setup(x => x.Users).Returns(mockDbSet.Object);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().Contain("Username is already taken.");
        }

        [Fact]
        public async Task Handle_ValidRequest_HashesPassword()
        {
            // Arrange
            var command = new RegisterCommand(
                "testuser",
                "test@example.com",
                "Test@1234");

            var users = new List<User>();
            var mockDbSet = users.AsQueryable().BuildMockDbSet();

            MockContext.Setup(x => x.Users).Returns(mockDbSet.Object);
            MockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert — verify Hash was called with the plain text password
            MockPasswordHasher.Verify(
                x => x.Hash("Test@1234"),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequest_SavesUserToDatabase()
        {
            // Arrange
            var command = new RegisterCommand(
                "testuser",
                "test@example.com",
                "Test@1234");

            var users = new List<User>();
            var mockDbSet = users.AsQueryable().BuildMockDbSet();

            MockContext.Setup(x => x.Users).Returns(mockDbSet.Object);
            MockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert — verify SaveChangesAsync was called exactly once
            MockContext.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
