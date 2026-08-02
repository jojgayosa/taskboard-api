using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Common.Models;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Features.Columns.Commands
{
    public record DeleteColumnCommand(
        Guid Id,
        Guid ProjectId) : IRequest<Result>;

    public class DeleteColumnCommandHandler : IRequestHandler<DeleteColumnCommand, Result>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public DeleteColumnCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IMapper mapper)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(DeleteColumnCommand request, CancellationToken cancellationToken)
        {
            var column = await _context.TaskColumns
                .Include(c => c.Project)
                .ThenInclude(p => p.Members)
                .Include(c => c.Tasks)
                .ThenInclude(t => t.Comments)
                .FirstOrDefaultAsync(c =>
                c.Id == request.Id && c.ProjectId == request.ProjectId, cancellationToken)
                ?? throw new NotFoundException(nameof(TaskColumn), request.Id);

            //Verify project member access
            var isMember = column.Project!.Members.Any(m => m.UserId == _currentUser.UserId);

            if (!isMember)
                throw new ForbiddenException("You are not a member of this project.");

            //Soft delete
            foreach (var task in column.Tasks)
            {
                foreach (var comment in task.Comments)
                {
                    comment.Delete();
                }
                task.Delete();
            }
            column.Delete();
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
