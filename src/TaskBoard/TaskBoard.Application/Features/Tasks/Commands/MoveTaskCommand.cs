using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Common.Models;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Features.Tasks.Commands
{
    public record MoveTaskCommand(
        Guid TaskId,
        Guid TargetColumnId) : IRequest<Result>;

    public class MoveTaskCommandValidator : AbstractValidator<MoveTaskCommand>
    {
        public MoveTaskCommandValidator() 
        {
            RuleFor(x => x.TaskId)
                .NotEmpty().WithMessage("Task Id is required.");

            RuleFor(x => x.TargetColumnId)
                .NotEmpty().WithMessage("Target column id is required.");
        }
    }

    public class MoveTaskCommandHandler : IRequestHandler<MoveTaskCommand, Result>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public MoveTaskCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(MoveTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _context.Tasks
            .Include(t => t.Column)
                .ThenInclude(c => c.Project)
                    .ThenInclude(p => p.Members)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

            var isMember = task.Column!.Project!.Members
                .Any(m => m.UserId == _currentUser.UserId);

            if (!isMember)
                throw new ForbiddenException("You are not a member of this project.");

            // Verify target column belongs to the same project
            var targetColumn = await _context.TaskColumns
                .FirstOrDefaultAsync(c =>
                    c.Id == request.TargetColumnId &&
                    c.ProjectId == task.Column.ProjectId, cancellationToken)
                ?? throw new NotFoundException(nameof(TaskColumn), request.TargetColumnId);

            var oldColumnName = task.Column.Name;

            task.ColumnId = request.TargetColumnId;

            // Log the move
            var activityLog = ActivityLog.Create(
                task.Id,
                $"Task moved from '{oldColumnName}' to '{targetColumn.Name}'.",
                _currentUser.UserId!.Value);

            _context.ActivityLogs.Add(activityLog);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();

        }
    }
}
