namespace TaskBoard.Domain.Common
{
    public interface ISoftDelete
    {
        bool IsDeleted {  get; }
        void Delete();
        void Restore();
    }
}
