namespace HackerNewsBestStories.Core.Common.Results;

using System.Reflection;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }

    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null)
        {
            throw new InvalidOperationException("A successful result cannot carry an error.");
        }

        if (!isSuccess && error is null)
        {
            throw new InvalidOperationException("A failed result must carry an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, null);

    public static Result<T> Failure<T>(Error error) => new(default, false, error);

    public static Result<T> CreateFailure<T>(Error error) => Failure<T>(error);

    internal static readonly MethodInfo CreateFailureMethod =
        typeof(Result).GetMethod(nameof(CreateFailure), BindingFlags.Public | BindingFlags.Static)!;
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    internal Result(T? value, bool isSuccess, Error? error) : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(value, true, null);

    public new static Result<T> Failure(Error error) => new(default, false, error);
}
