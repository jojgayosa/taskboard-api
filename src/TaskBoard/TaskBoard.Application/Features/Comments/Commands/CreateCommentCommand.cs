using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens.Experimental;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Common.Models;
using TaskBoard.Application.Features.Comments.DTOs;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Features.Comments.Commands
{
    public record CreateCommentCommand(
        Guid TaskId,
        string Message) : IRequest<Result<CommentResponseDto>>;

    public class CreateCommentCommandValidator : AbstractValidator<CommentResponseDto>
    {
        public CreateCommentCommandValidator() 
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Task Id is required.");

            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Message is required.")
                .MaximumLength(2000).WithMessage("Message cannot exceed 2000 characters.");
        }
    }

    public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, Result<CommentResponseDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CreateCommentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<CommentResponseDto>> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
        {
            var task = await _context.Tasks
                .Include(t => t.Column)
                    .ThenInclude(c => c.Project)
                        .ThenInclude(p => p.Members)
                .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
                ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

            //Check member access
            var isMember = task.Column!.Project!.Members.Any(m => m.UserId == _currentUser.UserId);
            if (!isMember)
                throw new ForbiddenException("You are not member of this project.");

            var comment = new Comment
            {
                TaskId = request.TaskId,
                UserId = _currentUser.UserId!.Value,
                Message = request.Message,
            };

            var log = ActivityLog.Create(
                task.Id,
                $"Comment added by user '{_currentUser.UserId}'",
                _currentUser.UserId!.Value);

            _context.Comments.Add(comment);
            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);

            // Get username for response
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

            return Result<CommentResponseDto>.Success(new CommentResponseDto
            {
                Id = comment.Id,
                TaskId = comment.TaskId,
                UserId = comment.UserId,
                Username = user?.Username ?? string.Empty,
                Message = comment.Message,
                CreatedDate = comment.CreatedDate
            });
        }
    }
}
