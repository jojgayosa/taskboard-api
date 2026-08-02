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
using TaskBoard.Application.Features.Columns.DTOs;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Features.Columns.Queries
{
    public record GetColumnsByProjectQuery(Guid ProjectId) : IRequest<Result<List<ColumnDto>>>;

    public class GetColumnsByProjectQueryHandler : IRequestHandler<GetColumnsByProjectQuery, Result<List<ColumnDto>>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetColumnsByProjectQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<List<ColumnDto>>> Handle(GetColumnsByProjectQuery request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
                ?? throw new NotFoundException(nameof(Project), request.ProjectId);

            var isMember = project.Members.Any(m => m.UserId == _currentUser.UserId);

            if (!isMember)
                throw new ForbiddenException("You are not member of this project.");

            var columns = await _context.TaskColumns
                .Where(c => c.ProjectId == request.ProjectId)
                .OrderBy(c => c.Order)
                .Select(c => new ColumnDto
                {
                    Id = c.Id,
                    ProjectId = c.ProjectId,
                    Name = c.Name,
                    Order = c.Order,
                    TaskCount = c.Tasks.Count()
                }).ToListAsync(cancellationToken);

            return Result<List<ColumnDto>>.Success(columns);
        }
    }
}
