using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Common.Models;
using TaskBoard.Application.Features.Projects.DTOs;

namespace TaskBoard.Application.Features.Projects.Queries
{
    public record GetProjectsQuery : IRequest<Result<List<ProjectDto>>>;

    public class GetProjectQeryHandler : IRequestHandler<GetProjectsQuery, Result<List<ProjectDto>>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IMapper _mapper;

        public GetProjectQeryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IMapper mapper)
        {
            _context = context;
            _currentUser = currentUser;
            _mapper = mapper;
        }

        public async Task<Result<List<ProjectDto>>> Handle(
            GetProjectsQuery request, 
            CancellationToken cancellationToken)
        {
            //Only return project the current user is related
            var projects = await _context.Projects
                .Where(p => p.Members.Any(m => m.UserId == _currentUser.UserId))
                .ProjectTo<ProjectDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return Result<List<ProjectDto>>.Success(projects);
        }
    }
}
