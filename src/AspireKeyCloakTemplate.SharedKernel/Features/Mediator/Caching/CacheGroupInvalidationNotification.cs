namespace AspireKeyCloakTemplate.SharedKernel.Features.Mediator.Caching;

/// <summary>
///     Represents a notification to invalidate a group of cache items.
/// </summary>
/// <param name="CacheGroupKey">The key of the cache group to invalidate.</param>
public record CacheGroupInvalidationNotification(string CacheGroupKey) : INotification;
