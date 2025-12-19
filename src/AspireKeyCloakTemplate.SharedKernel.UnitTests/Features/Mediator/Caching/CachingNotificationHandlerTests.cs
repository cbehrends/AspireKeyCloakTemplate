using System.Text;
using System.Text.Json;
using AspireKeyCloakTemplate.SharedKernel.Features.Mediator.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AspireKeyCloakTemplate.SharedKernel.UnitTests.Features.Mediator.Caching;

public class CachingNotificationHandlerTests
{
    private readonly IDistributedCache _cache;
    private readonly CacheGroupInvalidationNotificationHandler _groupInvalidationHandler;
    private readonly CacheInvalidationNotificationHandler _invalidationHandler;
    private readonly ILogger<CacheGroupInvalidationNotificationHandler> _loggerGroupInvalidation;
    private readonly ILogger<CacheInvalidationNotificationHandler> _loggerInvalidation;

    public CachingNotificationHandlerTests()
    {
        _cache = Substitute.For<IDistributedCache>();
        _loggerInvalidation = Substitute.For<ILogger<CacheInvalidationNotificationHandler>>();
        _loggerGroupInvalidation = Substitute.For<ILogger<CacheGroupInvalidationNotificationHandler>>();
        _invalidationHandler = new CacheInvalidationNotificationHandler(_cache, _loggerInvalidation);
        _groupInvalidationHandler = new CacheGroupInvalidationNotificationHandler(_cache, _loggerGroupInvalidation);
    }

    [Fact]
    public async Task Handle_CacheInvalidationNotification_Should_RemoveCacheKey()
    {
        // Arrange
        var notification = new CacheInvalidationNotification("test_key");

        // Act
        await _invalidationHandler.Handle(notification, CancellationToken.None);

        // Assert
        await _cache.Received(1).RemoveAsync(notification.CacheKey, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CacheGroupInvalidationNotification_Should_RemoveAllKeysInGroup()
    {
        // Arrange
        var groupKey = "group_key";
        var cacheKeys = new HashSet<string> { "key1", "key2" };
        var serializedCacheKeys = JsonSerializer.Serialize(cacheKeys);
        var notification = new CacheGroupInvalidationNotification(groupKey);

        // GetStringAsync is an extension method that calls GetAsync internally
        // Need to mock GetAsync and return the string as UTF8 bytes
        _cache.GetAsync(groupKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<byte[]?>(Encoding.UTF8.GetBytes(serializedCacheKeys)));

        // Act
        await _groupInvalidationHandler.Handle(notification, CancellationToken.None);

        // Assert
        await _cache.Received(1).RemoveAsync("key1", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync("key2", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync(groupKey, Arg.Any<CancellationToken>());
    }
}
