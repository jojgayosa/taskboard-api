using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Common.Models;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Features.Members.Commands
{
    public record RemoveMemberCommand(
        Guid ProjectId,
        Guid UserId) : IRequest<Result>;

    public class RemoveMemberCommandHandler : IRequestHandler<RemoveMemberCommand, Result>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        public RemoveMemberCommandHandler(ICurrentUserService currentUser, IApplicationDbContext context)
        {
            _context = context;
            _currentUser = currentUser;
        }
        public async Task<Result> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId,cancellationToken)
                ?? throw new NotFoundException(nameof(Projects),request.ProjectId);

            //Only project owner can remove member
            var isOwner = project.Members
                .Any(m => m.UserId == request.UserId && m.Role == ProjectRole.Owner);

            if (!isOwner)
                throw new ForbiddenException("Only the project owner can remove members.");

            //Not allowed to remove project owner
            var memberToRemove = project.Members.FirstOrDefault(m => m.UserId == request.UserId)
                ?? throw new NotFoundException(nameof(Members),request.UserId);

            if (memberToRemove.Role == ProjectRole.Owner)
                return Result.Failure("Cannot remove the project owner.");

            _context.ProjectMembers.Remove(memberToRemove);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
