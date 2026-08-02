using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Common.Models;

namespace TaskBoard.Application.Features.Tasks.Commands
{
    public record DeleteTaskCommand(Guid Id) : IRequest<Result>;

    public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, Result>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public DeleteTaskCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }
        public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _context.Tasks
                .Include(t => t.Column)
                    .ThenInclude(c => c.Project)
                        .ThenInclude(p => p.Members)
                .Include(t => t.Comments)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(Task), request.Id);

            //Check member access
            var isMember = task.Column!.Project!.Members.Any(m => m.UserId == _currentUser.UserId);
            if (!isMember) 
                throw new ForbiddenException("You are not member of this project");

            foreach (var comment in task.Comments)
                comment.Delete();
            task.Delete();

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
