using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Common.Models;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Features.Projects.Commands
{
    public record DeleteProjectCommand(Guid Id) : IRequest<Result>;

    public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, Result>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public DeleteProjectCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects
                .Include(p => p.Columns)
                .ThenInclude(c => c.Tasks)
                .ThenInclude(t => t.Comments)
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(Project), request.Id);

            if (project.OwnerId != _currentUser.UserId)
                throw new ForbiddenException("Only the project owner can delete this project.");

            //Cascade soft delete - mark everything under this project as deleted
            foreach (var column in project.Columns)
            {
                foreach (var task in column.Tasks)
                {
                    foreach (var comment in task.Comments)
                    {
                        comment.Delete();
                    }
                    task.Delete();
                }
                column.Delete();
            }
            project.Delete();

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
