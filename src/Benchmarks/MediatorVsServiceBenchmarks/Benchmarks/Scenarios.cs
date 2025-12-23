using AspireKeyCloakTemplate.SharedKernel.Features.Mediator;
using Microsoft.Extensions.DependencyInjection;
using BenchmarkDotNet.Attributes;

namespace MediatorVsServiceBenchmarks.Benchmarks;

public class Scenarios
{
    private ServiceProvider _provider = null!;
    private IMediator _mediator = null!;
    private IDirectTestService _directService = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        // Provide logging so pipeline behaviors that depend on ILogger can be activated
        services.AddLogging();

        // Register mediator and handlers from the shared kernel assembly
        services.AddMediator(typeof(AspireKeyCloakTemplate.SharedKernel.Features.Mediator.IMediator).Assembly);

        // Register a test handler and the equivalent direct service implementation
        services.AddScoped<IRequestHandler<TestRequest, TestResponse>, TestHandler>();
        services.AddScoped<IDirectTestService, DirectTestService>();

        services.AddScoped<IMediator, AspireKeyCloakTemplate.SharedKernel.Features.Mediator.Mediator>();

        _provider = services.BuildServiceProvider();

        _mediator = _provider.GetRequiredService<IMediator>();
        _directService = _provider.GetRequiredService<IDirectTestService>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _provider.Dispose();
    }

    [Benchmark(Baseline = true)]
    public async Task<TestResponse> DirectService_Invoke()
    {
        return await _directService.Handle(new TestRequest { Value = 42 }, CancellationToken.None);
    }

    [Benchmark]
    public async Task<TestResponse> Mediator_Invoke()
    {
        return await _mediator.Send<TestResponse>(new TestRequest { Value = 42 }, CancellationToken.None);
    }
}

public record TestRequest : IRequest<TestResponse>
{
    public int Value { get; init; }
}

public record TestResponse(int Value);

public interface IDirectTestService
{
    Task<TestResponse> Handle(TestRequest request, CancellationToken cancellationToken);
}

public class DirectTestService : IDirectTestService
{
    public Task<TestResponse> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        // Minimal logic to represent work
        return Task.FromResult(new TestResponse(request.Value + 1));
    }
}

public class TestHandler : IRequestHandler<TestRequest, TestResponse>
{
    public Task<TestResponse> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TestResponse(request.Value + 1));
    }
}
