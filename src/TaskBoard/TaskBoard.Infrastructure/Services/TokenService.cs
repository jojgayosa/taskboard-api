using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly string _secret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessTokenExpirationMinutes;
        private readonly int _refreshTokenExpirationDays;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
            _secret = configuration["JwtSettings:Secret"]
                ?? throw new InvalidOperationException("JWT Secret is not configured.");
            _issuer = configuration["JwtSettings:Issuer"]
                ?? throw new InvalidOperationException("JWT Issuer is not configured.");
            _audience = configuration["JwtSettings:Audience"]
                ?? throw new InvalidOperationException("JWT Audience is not configured.");
            _accessTokenExpirationMinutes = int.Parse(
                configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "15");
            _refreshTokenExpirationDays = int.Parse(
                configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7");
        }

        public string GenerateAccessToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            return GenerateToken(claims, TimeSpan.FromMinutes(_accessTokenExpirationMinutes));
        }

        public string GenerateRefreshToken(User user)
        {
            // Refresh token contains minimal claims — just enough to identify the user
            // and verify it's a refresh token (not an access token being misused)
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("token_type", "refresh"),
            };

            return GenerateToken(claims, TimeSpan.FromDays(_refreshTokenExpirationDays));
        }

        public Guid? ValidateRefreshToken(string refreshToken)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_secret);

                // First — let's read the token WITHOUT validating to see what's inside
                var unvalidatedToken = tokenHandler.ReadJwtToken(refreshToken);
                Console.WriteLine($"Token expires: {unvalidatedToken.ValidTo}");
                Console.WriteLine($"Token issued: {unvalidatedToken.ValidFrom}");
                Console.WriteLine($"Token issuer: {unvalidatedToken.Issuer}");
                Console.WriteLine($"Token type claim: {unvalidatedToken.Claims.FirstOrDefault(c => c.Type == "token_type")?.Value}");
                Console.WriteLine($"Current UTC time: {DateTime.UtcNow}");
                Console.WriteLine($"Secret being used: {_secret}");

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // no grace period on refresh tokens
                };

                var principal = tokenHandler.ValidateToken(
                    refreshToken,
                    validationParameters,
                    out _);

                // Make sure this is actually a refresh token, not an access token
                var tokenType = principal.FindFirstValue("token_type");
                if (tokenType != "refresh")
                    return null;

                var userIdClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (Guid.TryParse(userIdClaim, out var userId))
                    return userId;

                return null;
            }
            catch(Exception ex)
            {
                // Any validation failure (expired, tampered, wrong key) returns null
                Console.WriteLine($"Token validation failed: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        private string GenerateToken(IEnumerable<Claim> claims, TimeSpan expiration)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.Add(expiration),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
