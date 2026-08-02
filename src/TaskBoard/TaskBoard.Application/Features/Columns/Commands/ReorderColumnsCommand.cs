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
    public record ReorderColumnsCommand(
        Guid ProjectId,
        List <ReorderColumnDto> Columns) : IRequest<Result>;

    public class ReorderColumnsCommandValidator : AbstractValidator<ReorderColumnsCommand>
    {
        public ReorderColumnsCommandValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("Project Id is required");

            RuleFor(x => x.Columns)
                .NotEmpty().WithMessage("Column list is required.");
        }
    }

    public class ReorderColumnsCommandHandler : IRequestHandler<ReorderColumnsCommand, Result>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        public ReorderColumnsCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IMapper mapper) 
        { 
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(ReorderColumnsCommand request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects
                .Include(p => p.Members)
                .Include(p => p.Columns)
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
                ?? throw new NotFoundException(nameof(Project), request.ProjectId);

            var isMember = project.Members
                .Any(m => m.UserId == _currentUser.UserId);

            if (!isMember)
                throw new ForbiddenException("You are not a member of this project.");

            // Update order for each column
            foreach (var reorderItem in request.Columns)
            {
                var column = project.Columns
                    .FirstOrDefault(c => c.Id == reorderItem.ColumnId);

                if (column is not null)
                    column.Order = reorderItem.NewOrder;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
