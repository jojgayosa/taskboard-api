using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Common.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken(User user);
        Guid? ValidateRefreshToken(string refreshToken);
    }
}
