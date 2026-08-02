using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Common.Models;
using TaskBoard.Application.Features.Tasks.DTOs;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Features.Tasks.Commands
{
    public record CreateTaskCommand(
        Guid ColumnId,
        string Title,
        string? Description,
        Priority Priority,
        DateTime? DueDate,
        Guid? AssignedUserId) : IRequest<Result<TaskDto>>;

    public class CreateTaskCommandValidator : AbstractValidator<TaskDto>
    {
        public CreateTaskCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Task title is required.")
                .MaximumLength(200).WithMessage("Task title cannot exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Task description cannot exceed 2000 characters.")
                .When(x => x.Description is not null);

            RuleFor(x => x.Priority)
                .IsInEnum().WithMessage("Invalid priority value.");

            RuleFor(x => x.DueDate)
                .GreaterThanOrEqualTo(DateTime.UtcNow)
                .WithMessage("Duedate cannot be earlier than today.");
        }
    }

    public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, Result<TaskDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CreateTaskCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<TaskDto>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
        {
            var column = await _context.TaskColumns
                .Include(c => c.Project)
                    .ThenInclude(p => p.Members)
                .FirstOrDefaultAsync(c => c.Id == request.ColumnId, cancellationToken)
                ?? throw new NotFoundException(nameof(TaskColumn), request.ColumnId);

            //Check member access
            var isMember = column.Project!.Members.Any(m => m.UserId == _currentUser.UserId);
            if (!isMember)
                throw new ForbiddenException("You are not member of this project.");

            //Validate assigned user
            if (request.AssignedUserId.HasValue)
            {
                var isAssined = column.Project.Members.Any(m => m.UserId == request.AssignedUserId);
                if (!isAssined)
                    return Result<TaskDto>.Failure("Assigned user is not a member of this project.");
            }

            var task = new TaskItem
            {
                ColumnId = column.Id,
                Title = request.Title,
                Description = request.Description,
                Priority = request.Priority,
                DueDate = request.DueDate,
                AssignedUserId = request.AssignedUserId
            };

            //Log activity
            var activityLog = ActivityLog.Create(task.Id, $"Task '{task.Title}' was created.",_currentUser.UserId!.Value);

            _context.Tasks.Add(task);
            _context.ActivityLogs.Add(activityLog);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<TaskDto>.Success(
                new TaskDto
                {
                    Id = task.Id,
                    ColumnId = task.ColumnId,
                    Title = task.Title,
                    Description = task.Description,
                    Priority = task.Priority,
                    DueDate = task.DueDate,
                    AssignedUserId = task.AssignedUserId,
                    CommentCount = 0,
                    CreatedDate = task.CreatedDate,
                });
        }
    }
}
