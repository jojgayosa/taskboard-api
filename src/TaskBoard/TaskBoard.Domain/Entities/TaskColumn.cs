using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities
{
    public class TaskColumn : BaseEntity, ISoftDelete
    {
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsDeleted { get; set; }

        public Project? Project { get; set; }
        public ICollection<TaskItem> Tasks { get; set; } = [];

        public void Delete() => IsDeleted = true;
        public void Restore() => IsDeleted = false;
    }
}
