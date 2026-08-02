using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Common.Models;
using TaskBoard.Application.Features.Columns.DTOs;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Features.Columns.Commands
{
    public record CreateColumnCommand(
        Guid ProjectId,
        string Name) : IRequest<Result<ColumnDto>>;

    public class CreateColumnCommandValidator : AbstractValidator<CreateColumnCommand>
    {
        public CreateColumnCommandValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("Project Id is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Column name is required.")
                .MaximumLength(100).WithMessage("Column name cannot exceed 100 characters.");

        }
    }

    public class CreateCommandHandler : IRequestHandler<CreateColumnCommand, Result<ColumnDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IMapper _mapper;

        public CreateCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IMapper mapper)
        {
            _context = context;
            _currentUser = currentUser;
            _mapper = mapper;
        }

        public async Task<Result<ColumnDto>> Handle(CreateColumnCommand request, CancellationToken cancellationToken)
        {
            //Verify if project exist
            var project = await _context.Projects
                .Include(p => p.Members)
                .Include(p => p.Columns)
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
                ?? throw new NotFoundException(nameof(Project), request.ProjectId);

            //Verify current user is a member
            var isMember = project.Members.Any(m => m.UserId == _currentUser.UserId);

            if (!isMember)
                throw new ForbiddenException("You are not member of this project.");

            //Get current column order
            var maxOrder = project.Columns.Any() 
                ? project.Columns.Max(p => p.Order)
                : 0;

            //Check name if exist
            var isNameExist = project.Columns.Any(c => c.Name == request.Name);
            if (isNameExist)
                throw new InvalidOperationException("Column name already exist in this project.");


            var column = new TaskColumn
            {
                ProjectId = request.ProjectId,
                Name = request.Name,
                Order = maxOrder + 1,
            };

            _context.TaskColumns.Add(column);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<ColumnDto>.Success(
                new ColumnDto
                {
                    Id = column.Id,
                    ProjectId = column.ProjectId,
                    Name = column.Name,
                    Order = column.Order,
                    TaskCount = 0
                });
        }
    }
}
