using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Common.Models;
using TaskBoard.Application.Features.Comments.DTOs;
using TaskBoard.Application.Features.Tasks.DTOs;

namespace TaskBoard.Application.Features.Tasks.Queries
{
    public record GetTaskByIdQuery(Guid TaskId) : IRequest<Result<TaskDetailDto>>;

    public class GetTaskByIdQueryHandler : IRequestHandler<GetTaskByIdQuery, Result<TaskDetailDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        public GetTaskByIdQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<TaskDetailDto>> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
        {
            var task = await _context.Tasks
                .Include(t => t.Column)
                    .ThenInclude(c => c.Project)
                        .ThenInclude(p => p.Members)
                .Include(t => t.AssignedUser)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .Include(t => t.ActivityLogs)
                .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
                ?? throw new NotFoundException(nameof(Task), request.TaskId);

            //Check member access
            var isMember = task.Column!.Project!.Members.Any(m => m.UserId == _currentUser.UserId);
            if (isMember)
                throw new ForbiddenException("You are not a member of this project");

            return Result<TaskDetailDto>.Success(
                new TaskDetailDto
                {
                    Id = task.Id,
                    ColumnId = task.ColumnId,
                    ColumnName = task.Column.Name,
                    Title = task.Title,
                    Description = task.Description,
                    Priority = task.Priority,
                    DueDate = task.DueDate,
                    AssignedUserId = task.AssignedUserId,
                    AssignedUsername = task.AssignedUser?.Username,
                    CreatedDate = task.CreatedDate,
                    Comments = task.Comments
                    .OrderBy(c => c.CreatedDate)
                    .Select(c => new CommentDto
                    {
                        Id = c.Id,
                        UserId = c.UserId,
                        Username = c.User!.Username,
                        Message = c.Message,
                        CreatedDate = c.CreatedDate
                    }).ToList(),
                    ActivityLogs = task.ActivityLogs
                    .OrderByDescending(a => a.CreatedDate)
                    .Select(a => new ActivityLogDto
                    {
                        Id = a.Id,
                        Action = a.Action,
                        CreatedBy = a.CreatedBy,
                        CreatedDate = a.CreatedDate
                    }).ToList()
                });
        }
    }
}
