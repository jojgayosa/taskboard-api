using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities
{
    public class Comment : BaseEntity, ISoftDelete
    {
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
        public string Message { get; set; }
        public bool IsDeleted { get; set; }

        public TaskItem? Task { get; set; }
        public User? User { get; set; }

        public void Delete() => IsDeleted = true;
        public void Restore() => IsDeleted = false;
    }
}