using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Common.Models;
using TaskBoard.Application.Features.Auth.DTOs;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Features.Auth.Commands
{
    public record RegisterCommand(
        string Username,
        string Email,
        string Password) : IRequest<Result<AuthResponseDto>>;

    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
            .MaximumLength(50).WithMessage("Username cannot exceed 50 characters.")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("Username can only contain letters, numbers and underscores.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one number.");

        }
    }

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponseDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;

        public RegisterCommandHandler(
            IApplicationDbContext context,
            IPasswordHasher passwordHasher,
            ITokenService tokenService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        public async Task<Result<AuthResponseDto>> Handle(
            RegisterCommand request, 
            CancellationToken cancellationToken)
        {
            // Check if email exist
            var emailExist = await _context.Users
                .AnyAsync(u => u.Email == request.Email, cancellationToken);

            if (emailExist)
                return Result<AuthResponseDto>.Failure("Email already registered.");

            //Check if username already exist
            var nameExist = await _context.Users
                .AnyAsync(u => u.Username == request.Username, cancellationToken);

            if (nameExist) 
                return Result<AuthResponseDto>.Failure("Username already taken.");

            // Hash password first, then pass the hash to User.Create
            // Domain never sees the plain-text password
            var passwordHash = _passwordHasher.Hash(request.Password);
            var user = User.Create(request.Username, request.Email, passwordHash);

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken(user);

            return Result<AuthResponseDto>.Success(new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email
            });
        }
    }
}
