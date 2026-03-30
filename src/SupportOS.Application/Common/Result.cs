using MediatR;
using SupportOS.Domain.Common;

namespace SupportOS.Application.Common;

public interface IResult
{
    bool IsSuccess { get; }
    string? Error { get; }
    ErrorCode ErrorCode { get; }
    IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }
}

public class Result<T> : IResult
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public ErrorCode ErrorCode { get; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }

    protected Result(bool isSuccess, T? value, string? error, ErrorCode errorCode,
        IReadOnlyDictionary<string, string[]>? validationErrors = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorCode = errorCode;
        ValidationErrors = validationErrors;
    }

    public static Result<T> Success(T value) => new(true, value, null, ErrorCode.None);

    public static Result<T> Failure(string error, ErrorCode errorCode = ErrorCode.InvalidOperation)
        => new(false, default, error, errorCode);

    public static Result<T> ValidationFailure(IReadOnlyDictionary<string, string[]> errors)
        => new(false, default, "One or more validation errors occurred.", ErrorCode.ValidationFailed, errors);
}

public class Result : Result<Unit>
{
    private Result(bool isSuccess, string? error, ErrorCode errorCode,
        IReadOnlyDictionary<string, string[]>? validationErrors = null)
        : base(isSuccess, Unit.Value, error, errorCode, validationErrors) { }

    public static Result Success() => new(true, null, ErrorCode.None);

    public new static Result Failure(string error, ErrorCode errorCode = ErrorCode.InvalidOperation)
        => new(false, error, errorCode);

    public new static Result ValidationFailure(IReadOnlyDictionary<string, string[]> errors)
        => new(false, "One or more validation errors occurred.", ErrorCode.ValidationFailed, errors);
}
