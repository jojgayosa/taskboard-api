using TaskBoard.Domain.Common;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Domain.Entities
{
    public class ProjectMember : BaseEntity
    {
        public Guid ProjectId { get; private set; }
        public Guid UserId { get; private set; }
        public ProjectRole Role { get; private set; }

        public Project? Project { get; private set; }
        public User? User { get; private set; }

        public static ProjectMember Create(Guid projectId, Guid userId, ProjectRole role)
        {
            if (projectId == Guid.Empty)
                throw new ArgumentException("A valid project is required.", nameof(projectId));

            if (userId == Guid.Empty)
                throw new ArgumentException("A valid user is required.",nameof(userId));

            return new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                UserId = userId,
                Role = role
            };
        }

        public void ChangeRole(ProjectRole newRole)
        {
            Role = newRole;
        }
    }
}
