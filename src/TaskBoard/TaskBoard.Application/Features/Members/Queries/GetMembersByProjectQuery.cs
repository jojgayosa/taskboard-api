using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Common.Models;
using TaskBoard.Application.Features.Members.DTOs;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Features.Members.Queries
{
    public record GetMembersByProjectQuery(Guid ProjectId) : IRequest<Result<List<MemberDto>>>;

    public class GetMembersByProjectQueryHandler : IRequestHandler<GetMembersByProjectQuery, Result<List<MemberDto>>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        public GetMembersByProjectQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<List<MemberDto>>> Handle(GetMembersByProjectQuery request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects
                .Include(p => p.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
                ?? throw new NotFoundException(nameof(Project),request.ProjectId);

            //Check member access
            var isMember = project.Members.Any(m => m.UserId == _currentUser.UserId);
            if (!isMember)
                throw new ForbiddenException("You are not a member of this project.");

            var members = project.Members
                .Select(m => new MemberDto
                {
                    UserId = m.UserId,
                    Username = m.User!.Username,
                    Email = m.User.Email,
                    Role = m.Role,
                    JoinedDate = m.CreatedDate
                }).ToList();

            return Result<List<MemberDto>>.Success(members);
        }
    }
}
