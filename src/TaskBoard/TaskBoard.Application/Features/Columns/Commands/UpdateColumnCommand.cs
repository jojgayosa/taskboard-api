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
using TaskBoard.Application.Features.Columns.DTOs;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Features.Columns.Commands
{
    public record UpdateColumnCommand(
        Guid Id,
        Guid ProjectId,
        string Name) : IRequest<Result<ColumnDto>>;

    public class UpdateColumnCommandValidator : AbstractValidator<UpdateColumnCommand>
    {
        public UpdateColumnCommandValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("Project Id is required.");

            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Column Id is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Column name is required.")
                .MaximumLength(100).WithMessage("Column name cannot exceed 100 characters.");
        }
    }

    public class UpdateColumnCommandHandler : IRequestHandler<UpdateColumnCommand, Result<ColumnDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdateColumnCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IMapper mapper)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<ColumnDto>> Handle(UpdateColumnCommand request, CancellationToken cancellationToken)
        {
            //Verify if column exist
            var column = await _context.TaskColumns
                .Include(c => c.Project)
                .ThenInclude(p => p.Members)
                .Include(c => c.Tasks)
                .FirstOrDefaultAsync(c =>
                c.Id == request.Id &&
                c.ProjectId == request.ProjectId, cancellationToken)
                ?? throw new NotFoundException(nameof(TaskColumn), request.Id);

            //Verify current user project access
            var isMember = column.Project!.Members.Any(m => m.UserId == _currentUser.UserId);

            if (!isMember)
                throw new ForbiddenException("You are not member of this project");

            column.Name = request.Name;
            await _context.SaveChangesAsync(cancellationToken);

            return Result<ColumnDto>.Success(
                new ColumnDto
                {
                    Id = column.Id,
                    ProjectId = column.ProjectId,
                    Name = column.Name,
                    Order = column.Order,
                    TaskCount = column.Tasks.Count
                });
        }
    }
}
