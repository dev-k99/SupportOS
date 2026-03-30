using Microsoft.AspNetCore.Mvc;
using SupportOS.Domain.Common;
using AppResult = SupportOS.Application.Common.Result;

namespace SupportOS.API.Extensions;

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this SupportOS.Application.Common.Result<T> result, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return successStatusCode switch
            {
                StatusCodes.Status201Created => Results.Created(string.Empty, result.Value),
                StatusCodes.Status204NoContent => Results.NoContent(),
                _ => Results.Ok(result.Value)
            };
        }

        return MapError(result);
    }

    public static IResult ToHttpResult(this AppResult result, int successStatusCode = StatusCodes.Status204NoContent)
    {
        if (result.IsSuccess)
            return Results.NoContent();

        return MapError(result);
    }

    private static IResult MapError(SupportOS.Application.Common.IResult result)
    {
        if (result.ErrorCode == ErrorCode.ValidationFailed && result.ValidationErrors is not null)
            return Results.ValidationProblem(result.ValidationErrors.ToDictionary(k => k.Key, v => v.Value));

        return result.ErrorCode switch
        {
            ErrorCode.NotFound => Results.NotFound(new ProblemDetails
            {
                Title = "Resource not found.",
                Detail = result.Error,
                Status = StatusCodes.Status404NotFound
            }),
            ErrorCode.Forbidden => Results.Json(
                new ProblemDetails { Title = "Forbidden.", Detail = result.Error, Status = StatusCodes.Status403Forbidden },
                statusCode: StatusCodes.Status403Forbidden),
            ErrorCode.Unauthorized => Results.Unauthorized(),
            ErrorCode.AlreadyExists => Results.Conflict(new ProblemDetails
            {
                Title = "Conflict.",
                Detail = result.Error,
                Status = StatusCodes.Status409Conflict
            }),
            _ => Results.BadRequest(new ProblemDetails
            {
                Title = "Bad request.",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            })
        };
    }
}
