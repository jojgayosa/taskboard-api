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
    public record UpdateTaskCommand(
        Guid Id,
        string Title,
        string? Description,
        Priority Priority,
        DateTime? DueDate,
        Guid? AssignedUserId) : IRequest<Result<TaskDto>>;

    public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
    {
        public UpdateTaskCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Task title is required.")
                .MaximumLength(200).WithMessage("Task title cannot exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Task description cannot exceed 2,000 characters.")
                .When(x => x.Description is not null);

            RuleFor(x => x.Priority)
                .IsInEnum().WithMessage("Invalid priority value.");

            RuleFor(x => x.DueDate)
                .GreaterThanOrEqualTo(DateTime.UtcNow)
                .WithMessage("Duedate cannot be earlier than today.");

        }
    }

    public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, Result<TaskDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdateTaskCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }
        public async Task<Result<TaskDto>> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _context.Tasks
                .Include(t => t.Column)
                    .ThenInclude(c => c.Project)
                        .ThenInclude(p => p.Members)
                .Include(t => t.AssignedUser)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(TaskItem), request.Id);

            //Check project member access
            var isMember = task.Column!.Project!
                .Members.Any(m => m.UserId == _currentUser.UserId);

            if (!isMember)
                throw new ForbiddenException("You are not a member of this project.");

            if(request.AssignedUserId.HasValue)
            {
                var isAssigned = task.Column!.Project!.Members.Any(m => m.UserId == request.AssignedUserId);

                if (isAssigned)
                    return Result<TaskDto>.Failure("Assigned user is not a member of this project");
            }

            // Track what changed for activity log
            var changes = new List<string>();
            if (task.Title != request.Title)
                changes.Add($"Title changed from '{task.Title}' to '{request.Title}'.");
            if (task.Priority != request.Priority)
                changes.Add($"Priority changed from '{task.Priority}' to '{request.Priority}'.");
            if (task.AssignedUserId != request.AssignedUserId)
                changes.Add("Assignee was updated.");


            task.Title = request.Title;
            task.Description = request.Description;
            task.Priority = request.Priority;
            task.DueDate = request.DueDate;
            task.AssignedUserId = request.AssignedUserId;

            // Log activity if anything changed
            if (changes.Any())
            {
                var activityLog = ActivityLog.Create(
                    task.Id,
                    string.Join(" ", changes),
                    _currentUser.UserId!.Value);
                _context.ActivityLogs.Add(activityLog);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Result<TaskDto>.Success(
                new TaskDto
                {
                    Id = task.Id,
                    Title = task.Title,
                    Description = task.Description,
                    Priority = task.Priority,
                    DueDate = task.DueDate,
                    AssignedUserId = task.AssignedUserId,
                    AssignedUsername = task.AssignedUser?.Username,
                    CreatedDate = task.CreatedDate
                });
        }
    }
}
