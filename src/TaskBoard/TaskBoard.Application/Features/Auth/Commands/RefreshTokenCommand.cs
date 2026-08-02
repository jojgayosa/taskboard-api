using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Common.Models;
using TaskBoard.Application.Features.Auth.DTOs;

namespace TaskBoard.Application.Features.Auth.Commands
{
    public record RefreshTokenCommand(
    string RefreshToken) : IRequest<Result<AuthResponseDto>>;

    public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Refresh token is required.");
        }
    }

    public class RefreshTokenCommandHandler
        : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ITokenService _tokenService;

        public RefreshTokenCommandHandler(
            IApplicationDbContext context,
            ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        public async Task<Result<AuthResponseDto>> Handle(
            RefreshTokenCommand request,
            CancellationToken cancellationToken)
        {
            // Validate the refresh token and extract the userId from it
            var userId = _tokenService.ValidateRefreshToken(request.RefreshToken);

            if (userId is null)
                return Result<AuthResponseDto>.Failure("Invalid or expired refresh token.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user is null)
                return Result<AuthResponseDto>.Failure("User not found.");

            // Issue a fresh pair of tokens
            var newAccessToken = _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken(user);

            return Result<AuthResponseDto>.Success(new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email
            });
        }
    }
}
