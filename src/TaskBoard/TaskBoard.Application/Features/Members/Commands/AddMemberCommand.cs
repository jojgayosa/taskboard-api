using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Common.Models;
using TaskBoard.Application.Features.Members.DTOs;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Features.Members.Commands
{
    public record AddMemberCommand(
        Guid ProjectId,
        Guid UserId,
        ProjectRole Role = ProjectRole.Member) : IRequest<Result<MemberDto>>;

    public class AddMemberCommandValidator : AbstractValidator<AddMemberCommand>
    {
        public AddMemberCommandValidator() 
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("Project Id is required.");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User Id is required.");
        }
    }

    public class AddMemberCommandHandler : IRequestHandler<AddMemberCommand, Result<MemberDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        public AddMemberCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<MemberDto>> Handle(AddMemberCommand request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
                ?? throw new NotFoundException(nameof(Projects),request.ProjectId);

            //Only owner can add member
            var isOwner = project.Members
                .Any(m => m.UserId == _currentUser.UserId && m.Role == Domain.Enums.ProjectRole.Owner);

            if (!isOwner)
                throw new ForbiddenException("Only the owner of this project can add member");

            //Check if user is already a member
            var alreadyMember = project.Members.Any(m => m.UserId == request.UserId);

            if (alreadyMember)
                return Result<MemberDto>.Failure("User is already a member of this project.");

            //Verify user exist
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == request.UserId,cancellationToken)
                ?? throw new NotFoundException(nameof(User),request.UserId);

            var member = ProjectMember.Create(
                request.ProjectId,
                request.UserId,
                request.Role);

            _context.ProjectMembers.Add(member);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<MemberDto>.Success(new MemberDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = member.Role,
                JoinedDate = member.CreatedDate
            });
        }
    }
}

