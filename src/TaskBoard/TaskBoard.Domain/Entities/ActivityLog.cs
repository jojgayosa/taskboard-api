using System.Reflection;
using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities
{
    public class ActivityLog : BaseEntity
    {
        public Guid TaskId { get; set; }
        public string Action { get; set; }
        public Guid CreatedBy { get; set; }

        public TaskItem Task { get; set; }

        private ActivityLog() { }

        public static ActivityLog Create(Guid taskId, string action, Guid createdBy)
        {
            if (taskId == Guid.Empty)
                throw new ArgumentException("A valid task is required.", nameof(taskId));

            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("Action is required", nameof(action));

            return new ActivityLog
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                Action = action,
                CreatedBy = createdBy
            };
        }
    }
}