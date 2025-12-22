using AspireKeyCloakTemplate.BFF.Features.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AspireKeyCloakTemplate.BFF.UnitTests.Features.Core;

public class DistributedCacheTicketStoreTests
{
    private readonly IDistributedCache _cache;
    private readonly FakeLogger<DistributedCacheTicketStore> _logger;
    private readonly DistributedCacheTicketStore _store;

    public DistributedCacheTicketStoreTests()
    {
        _cache = Substitute.For<IDistributedCache>();
        _logger = new FakeLogger<DistributedCacheTicketStore>();
        _store = new DistributedCacheTicketStore(_cache, _logger);
    }

    [Fact]
    public async Task StoreAsync_ShouldStoreTicketAndReturnKey()
    {
        var ticket = CreateTicket();
        var key = await _store.StoreAsync(ticket);
        key.ShouldStartWith("AuthTicket-");
        await _cache.Received(1).SetAsync(key, Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>());
    }

    [Fact]
    public async Task RenewAsync_ShouldUpdateTicketInCache()
    {
        var ticket = CreateTicket();
        var key = "AuthTicket-test";
        await _store.RenewAsync(key, ticket);
        await _cache.Received(1).SetAsync(key, Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>());
    }

    [Fact]
    public async Task RenewAsync_ShouldLogRenewedTicket()
    {
        var ticket = CreateTicket();
        var key = "AuthTicket-log";
        await _store.RenewAsync(key, ticket);
        var log = _logger.Collector.GetSnapshot().SingleOrDefault(l => l.Message.Contains($"Authentication ticket renewed for key: {key} for user: testuser"));
        log.ShouldNotBeNull();
        log.Level.ShouldBe(LogLevel.Debug);
    }

    [Fact]
    public async Task RenewAsync_ShouldSetSlidingExpiration_WhenExpiresUtcNotSet()
    {
        var identity = new System.Security.Claims.ClaimsIdentity("TestAuthType");
        identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "testuser"));
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        var props = new AuthenticationProperties(); // ExpiresUtc not set
        var ticket = new AuthenticationTicket(principal, props, CookieAuthenticationDefaults.AuthenticationScheme);
        var key = "AuthTicket-sliding";

        await _store.RenewAsync(key, ticket);

        await _cache.Received(1).SetAsync(
            key,
            Arg.Any<byte[]>(),
            Arg.Is<DistributedCacheEntryOptions>(o => o.SlidingExpiration == TimeSpan.FromHours(1))
        );
    }

    [Fact]
    public async Task RetrieveAsync_ShouldReturnNullIfNotFound()
    {
        var key = "AuthTicket-missing";
        _cache.GetAsync(key).Returns((byte[])null!);
        var result = await _store.RetrieveAsync(key);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task RetrieveAsync_ShouldReturnTicketIfFound()
    {
        var ticket = CreateTicket();
        var key = "AuthTicket-found";
        var bytes = TicketSerializer.Default.Serialize(ticket);
        _cache.GetAsync(key).Returns(bytes);
        var result = await _store.RetrieveAsync(key);
        result.ShouldNotBeNull();
        result!.Principal.Identity!.Name.ShouldBe(ticket.Principal.Identity!.Name);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveTicketFromCache()
    {
        var key = "AuthTicket-remove";
        await _store.RemoveAsync(key);
        await _cache.Received(1).RemoveAsync(key);
    }

    private static AuthenticationTicket CreateTicket()
    {
        var identity = new System.Security.Claims.ClaimsIdentity("TestAuthType");
        identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "testuser"));
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        var props = new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) };
        return new AuthenticationTicket(principal, props, CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
