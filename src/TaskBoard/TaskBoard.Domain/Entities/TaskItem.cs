using TaskBoard.Domain.Common;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Domain.Entities
{
    public class TaskItem : BaseEntity, ISoftDelete
    {
        public Guid ColumnId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Priority Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid? AssignedUserId { get; set; }
        public bool IsDeleted { get; set; }

        public TaskColumn? Column { get; set; }
        public User? AssignedUser { get; set; }
        public ICollection<Comment> Comments { get; set; } = [];
        public ICollection<ActivityLog> ActivityLogs { get; set; } = [];

        public void Delete() => IsDeleted = true;
        public void Restore() => IsDeleted = false;
    }
}