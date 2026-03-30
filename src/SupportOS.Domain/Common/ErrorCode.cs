namespace SupportOS.Domain.Common;

public enum ErrorCode
{
    None = 0,
    ValidationFailed = 1,
    NotFound = 2,
    Unauthorized = 3,
    Forbidden = 4,
    AlreadyExists = 5,
    InvalidOperation = 6,
    InternalError = 7
}
