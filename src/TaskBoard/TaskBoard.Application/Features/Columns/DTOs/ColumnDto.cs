namespace TaskBoard.Application.Features.Columns.DTOs
{
    public class ColumnDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public int TaskCount { get; set; }
    }

    public class ReorderColumnDto
    {
        public Guid ColumnId { get; set; }
        public int NewOrder { get; set; }
    }
}
