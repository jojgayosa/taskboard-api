using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Common.Models;
using TaskBoard.Application.Features.Projects.DTOs;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Features.Projects.Queries
{
    public record GetProjectByIdQuery(Guid Id) : IRequest<Result<ProjectDetailDto>>;

    public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, Result<ProjectDetailDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IMapper _mapper;

        public GetProjectByIdQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IMapper mapper)
        {
            _context = context;
            _currentUser = currentUser;
            _mapper = mapper;
        }
        public async Task<Result<ProjectDetailDto>> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects
                .Include(p => p.Members)
                .Include(p => p.Columns.OrderBy(c => c.Order))
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(Project),request.Id);

            //Verify if the current user is a member of this project
            var isMember = project.Members.Any(m => m.UserId == _currentUser.UserId);

            if (!isMember)
                throw new ForbiddenException("You are not a member of this project");

            return Result<ProjectDetailDto>.Success(_mapper.Map<ProjectDetailDto>(project));
        }
    }
}
