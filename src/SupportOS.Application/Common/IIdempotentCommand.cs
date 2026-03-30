namespace SupportOS.Application.Common;

/// <summary>
/// Marks a command as idempotent. Commands implementing this interface carry a
/// client-generated key; identical keys within the TTL window return the cached
/// result instead of re-executing the handler.
/// </summary>
public interface IIdempotentCommand
{
    Guid IdempotencyKey { get; }
}
