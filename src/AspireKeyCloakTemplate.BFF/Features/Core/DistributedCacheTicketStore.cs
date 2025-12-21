using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;

namespace AspireKeyCloakTemplate.BFF.Features.Core;

/// <summary>
///     Implements server-side storage of authentication tickets using IDistributedCache.
///     This reduces cookie size by storing only a session identifier in the cookie while
///     keeping tokens and claims server-side.
///     Instrumented with OpenTelemetry metrics for cache hit/miss tracking.
/// </summary>
internal sealed partial class DistributedCacheTicketStore(
    IDistributedCache cache,
    ILogger<DistributedCacheTicketStore> logger) : ITicketStore
{
    private const string KeyPrefix = "AuthTicket-";

    // --- OpenTelemetry Instrumentation ---
    private static readonly Meter Meter = new("AspireKeyCloakTemplate.BFF", "1.0.0");

    private static readonly Counter<long> CacheHitsCounter =
        Meter.CreateCounter<long>(
            "bff.auth_cache.hits",
            "{hits}",
            "Number of authentication ticket cache hits");

    private static readonly Counter<long> CacheMissesCounter =
        Meter.CreateCounter<long>(
            "bff.auth_cache.misses",
            "{misses}",
            "Number of authentication ticket cache misses");

    private static readonly Counter<long> CacheStoresCounter =
        Meter.CreateCounter<long>(
            "bff.auth_cache.stores",
            "{stores}",
            "Number of authentication tickets stored");

    private static readonly Counter<long> CacheRemovalsCounter =
        Meter.CreateCounter<long>(
            "bff.auth_cache.removals",
            "{removals}",
            "Number of authentication tickets removed from cache");

    /// <summary>
    ///     Stores an authentication ticket in the distributed cache and returns a unique key.
    /// </summary>
    /// <param name="ticket">The authentication ticket to store.</param>
    /// <returns>A unique key that can be used to retrieve the ticket.</returns>
    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = KeyPrefix + Guid.NewGuid().ToString("N");
        await RenewAsync(key, ticket);
        CacheStoresCounter.Add(1);
        LogTicketStored(logger, key, ticket.Principal.Identity?.Name ?? "unknown");
        return key;
    }

    /// <summary>
    ///     Renews (updates) an existing authentication ticket in the cache.
    /// </summary>
    /// <param name="key">The key identifying the ticket.</param>
    /// <param name="ticket">The updated authentication ticket.</param>
    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var options = new DistributedCacheEntryOptions();
        var expiresUtc = ticket.Properties.ExpiresUtc;

        if (expiresUtc.HasValue)
            options.SetAbsoluteExpiration(expiresUtc.Value);
        else
            // Default expiration if not set
            options.SetSlidingExpiration(TimeSpan.FromHours(1));

        var serialized = TicketSerializer.Default.Serialize(ticket);
        await cache.SetAsync(key, serialized, options);
        LogTicketRenewed(logger, key, ticket.Principal.Identity?.Name ?? "unknown");
    }

    /// <summary>
    ///     Retrieves an authentication ticket from the cache by key.
    /// </summary>
    /// <param name="key">The key identifying the ticket.</param>
    /// <returns>The authentication ticket, or null if not found.</returns>
    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var bytes = await cache.GetAsync(key);
        if (bytes == null)
        {
            CacheMissesCounter.Add(1);
            LogTicketNotFound(logger, key);
            return null;
        }

        CacheHitsCounter.Add(1);
        var ticket = TicketSerializer.Default.Deserialize(bytes);
        LogTicketRetrieved(logger, key, ticket?.Principal.Identity?.Name ?? "unknown");
        return ticket;
    }

    /// <summary>
    ///     Removes an authentication ticket from the cache.
    /// </summary>
    /// <param name="key">The key identifying the ticket to remove.</param>
    public async Task RemoveAsync(string key)
    {
        await cache.RemoveAsync(key);
        CacheRemovalsCounter.Add(1);
        LogTicketRemoved(logger, key);
    }

    [LoggerMessage(LogLevel.Debug, "Authentication ticket stored with key: {key} for user: {userName}")]
    static partial void LogTicketStored(ILogger<DistributedCacheTicketStore> logger, string key, string userName);

    [LoggerMessage(LogLevel.Debug, "Authentication ticket renewed for key: {key} for user: {userName}")]
    static partial void LogTicketRenewed(ILogger<DistributedCacheTicketStore> logger, string key, string userName);

    [LoggerMessage(LogLevel.Debug, "Authentication ticket retrieved for key: {key} for user: {userName}")]
    static partial void LogTicketRetrieved(ILogger<DistributedCacheTicketStore> logger, string key, string userName);

    [LoggerMessage(LogLevel.Debug, "Authentication ticket not found for key: {key}")]
    static partial void LogTicketNotFound(ILogger<DistributedCacheTicketStore> logger, string key);

    [LoggerMessage(LogLevel.Debug, "Authentication ticket removed for key: {key}")]
    static partial void LogTicketRemoved(ILogger<DistributedCacheTicketStore> logger, string key);
}
