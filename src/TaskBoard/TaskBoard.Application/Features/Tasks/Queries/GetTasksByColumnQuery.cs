using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Common.Models;
using TaskBoard.Application.Features.Tasks.DTOs;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Features.Tasks.Queries
{
    public record GetTasksByColumnQuery(
        Guid ColumnId,
        Priority? Priority = null) : IRequest<Result<List<TaskDto>>>;

    public class GetTasksByColumnQueryHandler : IRequestHandler<GetTasksByColumnQuery, Result<List<TaskDto>>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        public GetTasksByColumnQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }
        public async Task<Result<List<TaskDto>>> Handle(GetTasksByColumnQuery request, CancellationToken cancellationToken)
        {
            var column = await _context.TaskColumns
                .Include(c => c.Project)
                    .ThenInclude(p => p.Members)
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(c => c.Id == request.ColumnId, cancellationToken)
                ?? throw new NotFoundException(nameof(TaskColumn), request.ColumnId);

            //Check member access
            var isMember = column.Project!.Members.Any(m => m.Id == _currentUser.UserId);
            if (isMember)
                throw new ForbiddenException("You are not a member of this project.");

            var query = _context.Tasks
                .Where(t => t.ColumnId == request.ColumnId);

            //Optional filter
            if (request.Priority.HasValue)
                query.Where(t => t.Priority == request.Priority);

            var tasks = await query
                .Select(t => new TaskDto
                {
                    Id = t.Id,
                    ColumnId = t.ColumnId,
                    Title = t.Title,
                    Description = t.Description,
                    Priority = t.Priority,
                    DueDate = t.DueDate,
                    AssignedUserId = t.AssignedUserId,
                    AssignedUsername = t.AssignedUser != null
                    ? t.AssignedUser.Username
                    : null,
                    CommentCount = t.Comments.Count(),
                    CreatedDate = t.CreatedDate
                })
                .ToListAsync(cancellationToken);

            return Result<List<TaskDto>>.Success(tasks);
        }
    }
}
