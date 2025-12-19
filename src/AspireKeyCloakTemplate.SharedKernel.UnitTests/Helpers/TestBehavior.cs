using AspireKeyCloakTemplate.SharedKernel.Features.Mediator;

namespace AspireKeyCloakTemplate.SharedKernel.UnitTests.Helpers;

public class TestPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public bool WasCalled { get; private set; }
    public TRequest? CapturedRequest { get; private set; }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        WasCalled = true;
        CapturedRequest = request;
        return await next();
    }
}

public class ModifyingBehavior : IPipelineBehavior<TestRequest, TestResponse>
{
    public async Task<TestResponse> Handle(
        TestRequest request,
        RequestHandlerDelegate<TestResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();
        return new TestResponse($"Modified: {response.Result}");
    }
}

public class ShortCircuitBehavior : IPipelineBehavior<TestRequest, TestResponse>
{
    public Task<TestResponse> Handle(
        TestRequest request,
        RequestHandlerDelegate<TestResponse> next,
        CancellationToken cancellationToken)
    {
        // Don't call next - short circuit the pipeline
        return Task.FromResult(new TestResponse("Short-circuited"));
    }
}

