namespace TaskBoard.Application.Common.Interfaces
{
    // Abstraction over HttpContext.User — lets handlers know who's making the request
    // without directly depending on ASP.NET Core (keeps Application framework-agnostic)
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        bool IsAuthenticated { get; }
    }
}
