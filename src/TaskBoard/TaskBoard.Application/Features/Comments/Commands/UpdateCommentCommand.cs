using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Common.Models;
using TaskBoard.Application.Features.Comments.DTOs;

namespace TaskBoard.Application.Features.Comments.Commands
{
    public record UpdateCommentCommand(Guid Id, string Message) : IRequest<Result<CommentResponseDto>>;

    public class UpdateCommentCommandValidator : AbstractValidator<UpdateCommentCommand>
    {
        public UpdateCommentCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Task Id is required.");

            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Message is required.")
                .MaximumLength(2000).WithMessage("Message cannot exceed 2000 characters.");
        }
    }

    public class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, Result<CommentResponseDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateCommentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Result<CommentResponseDto>> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
        {
            var comment = await _context.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(Comments), request.Id);

            //Only the author can delete its comment
            var isAuthor = comment.UserId == _currentUserService.UserId;
            if (!isAuthor)
                throw new ForbiddenException("Only the author comment can edit this comment.");

            comment.Message = request.Message;
            await _context.SaveChangesAsync(cancellationToken);

            return Result<CommentResponseDto>.Success(
                new CommentResponseDto
                {
                    Id = comment.Id,
                    TaskId = comment.TaskId,
                    UserId = comment.UserId,
                    Username = comment.User!.Username,
                    Message = comment.Message,
                    CreatedDate = comment.CreatedDate,
                });
        }
    }
}
