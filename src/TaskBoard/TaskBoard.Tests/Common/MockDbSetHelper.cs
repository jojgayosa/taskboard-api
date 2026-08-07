using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;

namespace TaskBoard.Tests.Common
{
    public static class MockDbSetHelper
    {
        public static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data)
        where T : class
        {
            return data.AsQueryable().BuildMockDbSet();
        }
    }
}
