using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Features.Members.DTOs
{
    public class MemberDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public ProjectRole Role { get; set; }
        public DateTime JoinedDate { get; set; }
    }
}
