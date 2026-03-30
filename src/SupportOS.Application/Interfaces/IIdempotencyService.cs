namespace SupportOS.Application.Interfaces;

public interface IIdempotencyService
{
    Task<(bool Found, TResponse? Result)> GetAsync<TResponse>(Guid key, CancellationToken cancellationToken = default);
    Task SetAsync<TResponse>(Guid key, TResponse result, TimeSpan expiry, CancellationToken cancellationToken = default);
}
