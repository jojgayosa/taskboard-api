using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Common.Models;
using TaskBoard.Application.Features.Comments.DTOs;

namespace TaskBoard.Application.Features.Comments.Queries
{
    public record GetCommentsByTaskQuery(Guid TaskId) : IRequest<Result<List<CommentResponseDto>>>;

    public class GetCommentsByTaskQueryHandler : IRequestHandler<GetCommentsByTaskQuery, Result<List<CommentResponseDto>>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetCommentsByTaskQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<List<CommentResponseDto>>> Handle(GetCommentsByTaskQuery request, CancellationToken cancellationToken)
        {
            var task = await _context.Tasks
                .Include(t => t.Column)
                    .ThenInclude(c => c.Project)
                        .ThenInclude(p => p.Members)
                .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
                ?? throw new NotFoundException(nameof(Task), request.TaskId); ;

            //Check member access
            var isMember = task.Column!.Project!.Members.Any(m => m.UserId == _currentUser.UserId);
            if (!isMember)
                throw new ForbiddenException("You are not a member of this project.");

            var comments = await _context.Comments
                .Include(c => c.User)
                .Where(c => c.TaskId == request.TaskId)
                .OrderBy(c => c.CreatedDate)
                .Select(c => new CommentResponseDto
                {
                    Id = c.Id,
                    TaskId = c.TaskId,
                    UserId = c.UserId,
                    Username = c.User!.Username,
                    Message = c.Message,
                    CreatedDate = c.CreatedDate,
                }).ToListAsync(cancellationToken);

            return Result<List<CommentResponseDto>>.Success(comments);
        }
    }
}
