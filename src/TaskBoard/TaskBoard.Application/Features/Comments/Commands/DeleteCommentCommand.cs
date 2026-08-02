using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Common.Models;

namespace TaskBoard.Application.Features.Comments.Commands
{
    public record DeleteCommentCommand(Guid Id) : IRequest<Result>;

    public class DeleteCommentCommantHandler : IRequestHandler<DeleteCommentCommand, Result>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public DeleteCommentCommantHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
        {
            var comment = await _context.Comments
                .Include(c => c.Task)
                    .ThenInclude(t => t.Column)
                        .ThenInclude(c => c.Project)
                            .ThenInclude(p => p.Members)
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(Comments), request.Id);

            //Only the author or project owner can delete
            var isAuthor = comment.UserId == _currentUser.UserId;
            var isOwner = comment.Task!.Column!.Project!.Members
                .Any(m => m.UserId == _currentUser.UserId &&
                        m.Role == Domain.Enums.ProjectRole.Owner);

            if(!isOwner && !isAuthor)
                throw new ForbiddenException("Only the comment author or project owner can delete this comment.");

            comment.Delete();
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
