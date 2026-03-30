using System.Reflection;
using FluentValidation;
using MediatR;
using SupportOS.Application.Common;

namespace SupportOS.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        // Group errors by property name for RFC 7807-compliant structured errors
        var fieldErrors = failures
            .GroupBy(f => f.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray(),
                StringComparer.OrdinalIgnoreCase);

        return CreateValidationFailureResult<TResponse>(fieldErrors);
    }

    private static TResponse CreateValidationFailureResult<T>(IReadOnlyDictionary<string, string[]> errors)
        where T : IResult
    {
        var responseType = typeof(T);

        if (responseType == typeof(Result))
            return (TResponse)(IResult)Result.ValidationFailure(errors);

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var innerType = responseType.GetGenericArguments()[0];
            var method = typeof(Result<>)
                .MakeGenericType(innerType)
                .GetMethod("ValidationFailure", BindingFlags.Public | BindingFlags.Static)!;
            return (TResponse)method.Invoke(null, new object[] { errors })!;
        }

        throw new InvalidOperationException($"Unsupported result type: {responseType.Name}");
    }
}
