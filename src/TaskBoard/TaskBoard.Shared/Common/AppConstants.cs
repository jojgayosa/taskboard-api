using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskBoard.Shared.Common
{
    public static class AppConstants
    {
        public static class Roles
        {
            public const string Owner = "Owner";
            public const string Member = "Member";
        }

        public static class Pagination
        {
            public const int DefaultPageSize = 10;
            public const int MaxPageSize = 50;
        }

        public static class Validation
        {
            public const int MaxNameLength = 200;
            public const int MaxDescriptionLength = 2000;
            public const int MaxCommentLength = 2000;
            public const int MinPasswordLength = 8;
            public const int MaxUsernameLength = 50;
        }
    }
}
