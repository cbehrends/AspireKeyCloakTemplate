using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AspireKeyCloakTemplate.SharedKernel.Features.Mediator.Caching;

public class CacheInvalidationNotificationHandler(
    IDistributedCache cache,
    ILogger<CacheInvalidationNotificationHandler> logger)
    : INotificationHandler<CacheInvalidationNotification>
{
    public async Task Handle(CacheInvalidationNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Invalidating cache for key {CacheKey}", notification.CacheKey);
        await cache.RemoveAsync(notification.CacheKey, cancellationToken);
    }
}
