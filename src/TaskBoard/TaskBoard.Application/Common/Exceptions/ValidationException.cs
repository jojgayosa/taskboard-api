using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskBoard.Application.Common.Exceptions
{
    public class ValidationException : Exception
    {
        public ValidationException(IEnumerable<string> errors)
        : base("One or more validation errors occurred.")
        {
            Errors = errors.ToList();
        }

        public List<string> Errors { get; }
    }
}
