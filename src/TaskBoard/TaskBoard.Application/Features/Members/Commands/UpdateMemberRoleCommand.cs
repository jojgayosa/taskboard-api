using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Common.Models;
using TaskBoard.Application.Features.Members.DTOs;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Features.Members.Commands
{
    public record UpdateMemberRoleCommand(
        Guid ProjectId,
        Guid UserId,
        ProjectRole NewRole) : IRequest<Result<MemberDto>>;

    public class UpdateMemberRoleCommandValidator : AbstractValidator<UpdateMemberRoleCommand>
    {
        public UpdateMemberRoleCommandValidator() 
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("Project Id is required.");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User Id is required.");
        }
    }

    public class UpdateMemberRoleCommandHandler : IRequestHandler<UpdateMemberRoleCommand, Result<MemberDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        public UpdateMemberRoleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<MemberDto>> Handle(UpdateMemberRoleCommand request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects
                .Include(p => p.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId,cancellationToken)
                ?? throw new NotFoundException(nameof(Projects),request.ProjectId);

            //Only project owner can change roles
            var isOwner = project.Members.Any(m => m.UserId == _currentUser.UserId && m.Role == ProjectRole.Owner);

            if (!isOwner)
                throw new ForbiddenException("Only the project owner can update member roles.");

            var member = project.Members.FirstOrDefault(m => m.UserId == request.UserId)
                ?? throw new NotFoundException(nameof(Members), request.UserId);

            member.ChangeRole(request.NewRole);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<MemberDto>.Success(new MemberDto
            {
                UserId = member.UserId,
                Username = member.User!.Username,
                Email = member.User!.Email,
                Role = member.Role,
                JoinedDate = member.CreatedDate
            });
        }
    }
}
