using MediatR;
using SupportOS.Application.Common;
using SupportOS.Application.Interfaces;

namespace SupportOS.Application.Behaviors;

/// <summary>
/// Checks whether an identical request (same IdempotencyKey) has already been
/// processed. If so, returns the cached result instead of re-executing the handler.
/// Only caches successful results — failed results are never replayed.
/// </summary>
public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult
{
    private readonly IIdempotencyService _idempotencyService;

    public IdempotencyBehavior(IIdempotencyService idempotencyService)
    {
        _idempotencyService = idempotencyService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IIdempotentCommand idempotentCommand)
            return await next();

        var (found, cached) = await _idempotencyService.GetAsync<TResponse>(idempotentCommand.IdempotencyKey, cancellationToken);
        if (found && cached is not null)
            return cached;

        var result = await next();

        if (result.IsSuccess)
            await _idempotencyService.SetAsync(idempotentCommand.IdempotencyKey, result, TimeSpan.FromHours(24), cancellationToken);

        return result;
    }
}
