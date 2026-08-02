using TaskBoard.Application.Features.Columns.DTOs;

namespace TaskBoard.Application.Features.Projects.DTOs
{
    public class ProjectDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid OwnerId { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    public class ProjectDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid OwnerId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int MemberCount { get; set; }
        public List<ColumnDto> Columns { get; set; } = new();
    }
}
