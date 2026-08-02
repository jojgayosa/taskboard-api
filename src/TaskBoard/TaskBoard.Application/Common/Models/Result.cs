using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskBoard.Application.Common.Models
{
    public class Result
    {
        public bool Succeeded { get; }
        public bool Failed => !Succeeded;
        public string[] Errors { get; }
        protected Result(bool succeed, IEnumerable<string> errors)
        {
            Succeeded = succeed;
            Errors = errors.ToArray();
        }

        public static Result Success() => new(true, Array.Empty<string>());

        public static Result Failure(params string[] errors) => new(false, errors);

    }

    public class Result<T> : Result
    {
        public T? Data { get; }
        protected Result(bool succeeded, T? data, IEnumerable<string> errors) : base(succeeded, errors)
        {
            Data = data;
        }

        public static Result<T> Success(T data) =>
        new(true, data, Array.Empty<string>());

        public static new Result<T> Failure(params string[] errors) =>
            new(false, default, errors);
    }
}
