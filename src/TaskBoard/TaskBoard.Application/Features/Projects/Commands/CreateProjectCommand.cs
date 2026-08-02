using AutoMapper;
using FluentValidation;
using MediatR;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Common.Models;
using TaskBoard.Application.Features.Projects.DTOs;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Features.Projects.Commands
{
    public record CreateProjectCommand(
        string Name,
        string? Description) : IRequest<Result<ProjectDto>>;

    public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
    {
        public CreateProjectCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Project name is required")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Desccription cannot exceed 1000 characters.")
                .When(x => x.Description is not null);
        }
    }

    public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Result<ProjectDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IMapper _mapper;

        public CreateProjectCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IMapper mapper)
        {
            _context = context;
            _currentUser = currentUser;
            _mapper = mapper;
        }

        public async Task<Result<ProjectDto>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
        {
            if (_currentUser.UserId is null)
                return Result<ProjectDto>.Failure("User is not authenticated.");

            var project = new Project
            {
                Name = request.Name,
                Description = request.Description,
                OwnerId = _currentUser.UserId.Value
            };

            //Automatically owner as member
            var ownerMember = ProjectMember.Create(
                project.Id,
                _currentUser.UserId.Value,
                ProjectRole.Owner);

            _context.Projects.Add(project);
            _context.ProjectMembers.Add(ownerMember);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<ProjectDto>.Success(_mapper.Map<ProjectDto>(project));
        }
    }
        
}
