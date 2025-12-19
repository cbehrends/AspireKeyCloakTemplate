namespace AspireKeyCloakTemplate.SharedKernel.Features.Mediator.Caching;

/// <summary>
///     Represents a request that can be cached.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface ICacheableRequest<out TResponse> : IRequest<TResponse>
{
    /// <summary>
    ///     Gets the key for the cache item.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    ///     Gets the key for the cache group. This is used to invalidate a group of cache items at once.
    /// </summary>
    string? CacheGroupKey { get; }

    /// <summary>
    ///     Gets the absolute expiration time relative to now.
    /// </summary>
    TimeSpan? AbsoluteExpirationRelativeToNow { get; }
}
