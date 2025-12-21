using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AspireKeyCloakTemplate.SharedKernel.Features.Mediator.Caching;

public partial class CacheGroupInvalidationNotificationHandler(
    IDistributedCache cache,
    ILogger<CacheGroupInvalidationNotificationHandler> logger)
    : INotificationHandler<CacheGroupInvalidationNotification>
{
    public async Task Handle(CacheGroupInvalidationNotification notification, CancellationToken cancellationToken)
    {
        LogInvalidatingCacheForGroupCachegroupkey(logger, notification.CacheGroupKey);
        var cachedGroup = await cache.GetStringAsync(notification.CacheGroupKey, cancellationToken);

        if (!string.IsNullOrEmpty(cachedGroup))
        {
            var cacheKeys = JsonSerializer.Deserialize<HashSet<string>>(cachedGroup);
            if (cacheKeys != null)
                foreach (var cacheKey in cacheKeys)
                    await cache.RemoveAsync(cacheKey, cancellationToken);

            await cache.RemoveAsync(notification.CacheGroupKey, cancellationToken);
        }
    }

    [LoggerMessage(LogLevel.Information, "Invalidating cache for group {CacheGroupKey}")]
    static partial void LogInvalidatingCacheForGroupCachegroupkey(ILogger<CacheGroupInvalidationNotificationHandler> logger, string CacheGroupKey);
}
