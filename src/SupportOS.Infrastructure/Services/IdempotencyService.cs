using Microsoft.Extensions.Caching.Memory;
using SupportOS.Application.Interfaces;

namespace SupportOS.Infrastructure.Services;

/// <summary>
/// In-process idempotency store backed by IMemoryCache.
/// For multi-instance deployments, replace with IDistributedCache (Redis).
/// </summary>
public sealed class IdempotencyService : IIdempotencyService
{
    private readonly IMemoryCache _cache;
    private const string KeyPrefix = "idempotency:";

    public IdempotencyService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<(bool Found, TResponse? Result)> GetAsync<TResponse>(Guid key, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{KeyPrefix}{key}";
        if (_cache.TryGetValue(cacheKey, out TResponse? cached))
            return Task.FromResult((true, cached));

        return Task.FromResult<(bool, TResponse?)>((false, default));
    }

    public Task SetAsync<TResponse>(Guid key, TResponse result, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{KeyPrefix}{key}";
        _cache.Set(cacheKey, result, expiry);
        return Task.CompletedTask;
    }
}
