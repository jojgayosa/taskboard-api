using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities
{
    public class Project : BaseEntity, ISoftDelete
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid OwnerId { get; set; }
        public bool IsDeleted { get; set; }

        public User? Owner { get; set; }
        public ICollection<ProjectMember> Members { get; set; } = [];
        public ICollection<TaskColumn> Columns { get; set; } = [];

        public void Delete() => IsDeleted = true;
        public void Restore() => IsDeleted = false;
    }
}
