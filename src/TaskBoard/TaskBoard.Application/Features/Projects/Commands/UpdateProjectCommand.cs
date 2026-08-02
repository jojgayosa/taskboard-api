using AutoMapper;
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
using TaskBoard.Application.Features.Projects.DTOs;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Features.Projects.Commands
{
    public record UpdateProjectCommand(
        Guid Id,
        string Name,
        string? Description) : IRequest<Result<ProjectDto>>;

    public class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
    {
        public UpdateProjectCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Project Id is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Project name is required.")
                .MaximumLength(200).WithMessage("Project name cannot exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.")
                .When(x => x.Description is not null);
        }
    }

    public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, Result<ProjectDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IMapper _mapper;

        public UpdateProjectCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IMapper mapper)
        {
            _context = context;
            _currentUser = currentUser;
            _mapper = mapper;
        }

        public async Task<Result<ProjectDto>> Handle(
            UpdateProjectCommand request, 
            CancellationToken cancellationToken)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(Project), request.Id);

            //Only the owner can update the project
            if (project.OwnerId != _currentUser.UserId)
                throw new UnauthorizedAccessException("Only the project owner can update this project.");

            project.Name = request.Name;
            project.Description = request.Description;

            await _context.SaveChangesAsync(cancellationToken);

            return Result<ProjectDto>.Success(_mapper.Map<ProjectDto>(project));
        }
    }
}
