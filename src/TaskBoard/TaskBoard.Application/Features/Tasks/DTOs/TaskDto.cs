using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskBoard.Application.Features.Comments.DTOs;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Features.Tasks.DTOs
{
    public class TaskDto
    {
        public Guid Id { get; set; }
        public Guid ColumnId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Priority Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid? AssignedUserId { get; set; }
        public string? AssignedUsername { get; set; }
        public int CommentCount { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class TaskDetailDto
    {
        public Guid Id { get; set; }
        public Guid ColumnId { get; set; }
        public string ColumnName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Priority Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid? AssignedUserId { get; set; }
        public string? AssignedUsername { get; set; }
        public DateTime CreatedDate { get; set; } 
        public List<CommentDto> Comments { get; set; } = new();
        public List<ActivityLogDto> ActivityLogs { get; set; } = new();
    }

    public class ActivityLogDto
    {
        public Guid Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public Guid CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
