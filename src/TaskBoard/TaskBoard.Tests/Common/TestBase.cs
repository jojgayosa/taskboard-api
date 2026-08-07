using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using TaskBoard.Application.Common.Interfaces;

namespace TaskBoard.Tests.Common
{
    public abstract class TestBase
    {
        // Shared mocks used across all tests
        protected readonly Mock<IApplicationDbContext> MockContext;
        protected readonly Mock<ICurrentUserService> MockCurrentUser;
        protected readonly Mock<ITokenService> MockTokenService;
        protected readonly Mock<IPasswordHasher> MockPasswordHasher;
        protected readonly Mock<ILogger<object>> MockLogger;
        protected readonly Mock<IMapper> MockMapper;

        // Hardcoded test data — consistent across all tests
        protected readonly Guid CurrentUserId = Guid.NewGuid();
        protected readonly Guid ProjectId = Guid.NewGuid();
        protected readonly Guid ColumnId = Guid.NewGuid();
        protected readonly Guid TaskId = Guid.NewGuid();
        protected readonly Guid MemberId = Guid.NewGuid();

        protected TestBase()
        {
            MockContext = new Mock<IApplicationDbContext>();
            MockCurrentUser = new Mock<ICurrentUserService>();
            MockTokenService = new Mock<ITokenService>();
            MockPasswordHasher = new Mock<IPasswordHasher>();
            MockLogger = new Mock<ILogger<object>>();
            MockMapper = new Mock<IMapper>();

            // Default: current user is authenticated
            MockCurrentUser.Setup(x => x.UserId).Returns(CurrentUserId);
            MockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

            // Default: password hasher returns predictable values
            MockPasswordHasher.Setup(x => x.Hash(It.IsAny<string>()))
                .Returns("hashed_password");
            MockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);

            // Default: token service returns predictable tokens
            MockTokenService.Setup(x => x.GenerateAccessToken(It.IsAny<Domain.Entities.User>()))
                .Returns("fake_access_token");
            MockTokenService.Setup(x => x.GenerateRefreshToken(It.IsAny<Domain.Entities.User>()))
                .Returns("fake_refresh_token");
        }
    }
}
