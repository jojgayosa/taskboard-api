using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TaskBoard.Application.Common.Interfaces;

namespace TaskBoard.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public Guid? UserId
        {
            get{
                // Try sub claim first (works when DefaultInboundClaimTypeMap is cleared)
                var userIdClaim = _httpContextAccessor.HttpContext?.User
                    .FindFirstValue(JwtRegisteredClaimNames.Sub);

                // Fallback to NameIdentifier (legacy remapped claim name)
                userIdClaim ??= _httpContextAccessor.HttpContext?.User
                    .FindFirstValue(ClaimTypes.NameIdentifier);

                if (Guid.TryParse(userIdClaim, out var userId))
                    return userId;

                return null;
            }
        }

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }
}
