using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using TaskBoard.Application.Features.Auth.Commands;
using TaskBoard.Domain.Entities;
using TaskBoard.Tests.Common;

namespace TaskBoard.Tests.Features.Auth
{
    public class LoginCommandHandlerTests : TestBase
    {
        private readonly LoginCommandHandler _handler;
        public LoginCommandHandlerTests() 
        {
            _handler = new LoginCommandHandler(
                MockContext.Object,
                MockPasswordHasher.Object,
                MockTokenService.Object);
        }

        [Fact]
        public async Task Handle_ValidCredentials_ReturnsSuccess()
        {
            //Arrange
            var user = User.Create("testuser", "test@example.com", "hashed_password");
            var users = new List<User> { user };
            var mockDbSet = users.AsQueryable().BuildMockDbSet();

            MockContext.Setup(x => x.Users).Returns(mockDbSet.Object);
            MockPasswordHasher.Setup(x => x.Verify("Test@1234", "hashed_password")).Returns(true);

            var command = new LoginCommand("test@example.com", "Test@1234");

            //Act
            var result = await _handler.Handle(command, CancellationToken.None);

            //Assert
            result.Succeeded.Should().BeTrue();
            result.Data!.AccessToken.Should().Be("fake_access_token");
            result.Data.RefreshToken.Should().Be("fake_refresh_token");
            result.Data.Email.Should().Be("test@example.com");
        }

        [Fact]
        public async Task Handle_UserNotFound_ReturnsFailure()
        {
            //Arrange
            var users = new List<User>();
            var mockDbSet = users.AsQueryable().BuildMockDbSet();

            MockContext.Setup(x => x.Users).Returns(mockDbSet.Object);

            var command = new LoginCommand("notuser@example.com", "Test@1234");

            //Act
            var result = await _handler.Handle(command, CancellationToken.None);

            //Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().Contain("Invalid email or password.");
        }

        [Fact]
        public async Task Handle_WrongPassword_ReturnsFailure()
        {
            //Arrange
            var user = User.Create("testuser", "test@example.com", "hashed_password");
            var users = new List<User> { user };

            MockContext.Setup(x => x.Users).Returns(users.AsQueryable().BuildMockDbSet().Object);

            MockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            var command = new LoginCommand("test@example.com", "wrong_password");

            //Act
            var result = await _handler.Handle(command,CancellationToken.None);

            //Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().Contain("Invalid email or password.");
        }

        [Fact]
        public async Task Handle_WrongPassword_DoesNotRevealWhichFieldFailed()
        {
            // Arrange — security test: error message must be generic
            // regardless of whether email or password was wrong
            var user = User.Create("testuser", "test@example.com", "hashed_password");
            var users = new List<User> { user };
            var mockDbSet = users.AsQueryable().BuildMockDbSet();

            MockContext.Setup(x => x.Users).Returns(mockDbSet.Object);
            MockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(false);

            var command = new LoginCommand("test@example.com", "WrongPassword");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert — must NOT say "wrong password" or "email not found"
            result.Errors.Should().NotContain(e => e.Contains("password") && !e.Contains("Invalid"));
            result.Errors.Should().NotContain(e => e.Contains("email") && !e.Contains("Invalid"));
            result.Errors.Should().Contain("Invalid email or password.");
        }
    }
}
