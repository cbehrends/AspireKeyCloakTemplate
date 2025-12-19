using System.Text.Json;
using AspireKeyCloakTemplate.SharedKernel.Features.Mediator.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AspireKeyCloakTemplate.SharedKernel.Features.Mediator.Behaviors;

public partial class CachingBehavior<TRequest, TResponse>(
    ILogger<CachingBehavior<TRequest, TResponse>> logger,
    IDistributedCache cache)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheableRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var cacheKey = request.CacheKey;
        var cachedResponse = await cache.GetAsync(cacheKey, cancellationToken);

        if (cachedResponse != null)
        {
            LogCacheHit(logger, cacheKey);
            return JsonSerializer.Deserialize<TResponse>(cachedResponse)!;
        }

        LogCacheMiss(logger, cacheKey);

        var response = await next();

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = request.AbsoluteExpirationRelativeToNow
        };

        var serializedResponse = JsonSerializer.SerializeToUtf8Bytes(response);
        await cache.SetAsync(cacheKey, serializedResponse, options, cancellationToken);

        if (request.CacheGroupKey != null)
        {
            var groupCacheKey = request.CacheGroupKey;
            var cachedGroup = await cache.GetStringAsync(groupCacheKey, cancellationToken);
            var cacheKeys = string.IsNullOrEmpty(cachedGroup)
                ? new HashSet<string>()
                : JsonSerializer.Deserialize<HashSet<string>>(cachedGroup)!;

            if (cacheKeys.Add(cacheKey))
                await cache.SetStringAsync(groupCacheKey, JsonSerializer.Serialize(cacheKeys), options,
                    cancellationToken);
        }

        return response;
    }

    [LoggerMessage(LogLevel.Information, "Cache hit for {CacheKey}")]
    static partial void LogCacheHit(ILogger<CachingBehavior<TRequest, TResponse>> logger, string cacheKey);

    [LoggerMessage(LogLevel.Information, "Cache miss for {CacheKey}")]
    static partial void LogCacheMiss(ILogger<CachingBehavior<TRequest, TResponse>> logger, string cacheKey);
}
