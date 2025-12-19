using System.Text.Json;
using AspireKeyCloakTemplate.SharedKernel.Features.Mediator.Behaviors;
using AspireKeyCloakTemplate.SharedKernel.Features.Mediator.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AspireKeyCloakTemplate.SharedKernel.UnitTests.Features.Mediator.Behaviors;

public class CachingBehaviorTests
{
    private readonly IDistributedCache _cache;
    private readonly CachingBehavior<TestCacheableRequest, TestResponse> _cachingBehavior;
    private readonly ILogger<CachingBehavior<TestCacheableRequest, TestResponse>> _logger;

    public CachingBehaviorTests()
    {
        _cache = Substitute.For<IDistributedCache>();
        _logger = Substitute.For<ILogger<CachingBehavior<TestCacheableRequest, TestResponse>>>();
        _cachingBehavior = new CachingBehavior<TestCacheableRequest, TestResponse>(_logger, _cache);
    }

    [Fact]
    public async Task Handle_Should_ReturnCachedResponse_When_CacheHit()
    {
        // Arrange
        var request = new TestCacheableRequest("test_key", null, TimeSpan.FromMinutes(5));
        var expectedResponse = new TestResponse("test_data");
        var cachedResponseBytes = JsonSerializer.SerializeToUtf8Bytes(expectedResponse);
        _cache.GetAsync(request.CacheKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<byte[]?>(cachedResponseBytes));

        // Act
        var response = await _cachingBehavior.Handle(request, () => Task.FromResult(new TestResponse("")),
            CancellationToken.None);

        // Assert
        response.Data.ShouldBe(expectedResponse.Data);
        await _cache.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ExecuteNext_And_CacheResponse_When_CacheMiss()
    {
        // Arrange
        var request = new TestCacheableRequest("test_key", null, TimeSpan.FromMinutes(5));
        var expectedResponse = new TestResponse("test_data");
        _cache.GetAsync(request.CacheKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<byte[]?>(null));

        // Act
        var response =
            await _cachingBehavior.Handle(request, () => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        response.Data.ShouldBe(expectedResponse.Data);
        await _cache.Received(1).SetAsync(request.CacheKey, Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_AddToCacheGroup_When_CacheGroupKeyIsProvided()
    {
        // Arrange
        var request = new TestCacheableRequest("test_key", "group_key", TimeSpan.FromMinutes(5));
        var expectedResponse = new TestResponse("test_data");
        _cache.GetAsync(request.CacheKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<byte[]?>(null));
        // GetStringAsync is an extension method that calls GetAsync internally
        // Return null bytes to simulate empty group cache
        _cache.GetAsync(request.CacheGroupKey!, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<byte[]?>(null));

        // Act
        await _cachingBehavior.Handle(request, () => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert - SetStringAsync is an extension method that calls SetAsync internally
        await _cache.Received(1).SetAsync(request.CacheGroupKey!, Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>());
    }

    public record TestCacheableRequest(
        string CacheKey,
        string? CacheGroupKey,
        TimeSpan? AbsoluteExpirationRelativeToNow) : ICacheableRequest<TestResponse>;

    public record TestResponse(string Data);
}
