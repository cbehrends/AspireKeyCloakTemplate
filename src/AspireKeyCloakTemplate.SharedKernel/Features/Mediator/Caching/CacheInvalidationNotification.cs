namespace AspireKeyCloakTemplate.SharedKernel.Features.Mediator.Caching;

/// <summary>
///     Represents a notification to invalidate a single cache item.
/// </summary>
/// <param name="CacheKey">The key of the cache item to invalidate.</param>
public record CacheInvalidationNotification(string CacheKey) : INotification;
