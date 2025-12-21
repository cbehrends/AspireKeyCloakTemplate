using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AspireKeyCloakTemplate.SharedKernel.Features.Mediator.Caching;

public partial class CacheInvalidationNotificationHandler(
    IDistributedCache cache,
    ILogger<CacheInvalidationNotificationHandler> logger)
    : INotificationHandler<CacheInvalidationNotification>
{
    public async Task Handle(CacheInvalidationNotification notification, CancellationToken cancellationToken)
    {
        LogInvalidatingCacheForKeyCachekey(logger, notification.CacheKey);
        await cache.RemoveAsync(notification.CacheKey, cancellationToken);
    }

    [LoggerMessage(LogLevel.Information, "Invalidating cache for key {CacheKey}")]
    static partial void LogInvalidatingCacheForKeyCachekey(ILogger<CacheInvalidationNotificationHandler> logger, string CacheKey);
}
