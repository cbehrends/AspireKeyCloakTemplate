using AspireKeyCloakTemplate.SharedKernel.Features.Mediator;

namespace AspireKeyCloakTemplate.SharedKernel.UnitTests.Helpers;

public class TestRequestHandler : IRequestHandler<TestRequest, TestResponse>
{
    public Task<TestResponse> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TestResponse($"Handled: {request.Value}"));
    }
}

public class TestVoidRequestHandler : IRequestHandler<TestVoidRequest>
{
    public bool WasCalled { get; private set; }

    public Task<Unit> Handle(TestVoidRequest request, CancellationToken cancellationToken)
    {
        WasCalled = true;
        return Task.FromResult(Unit.Value);
    }
}

public class GenericRequestHandler<T> : IRequestHandler<GenericRequest<T>, T>
{
    public Task<T> Handle(GenericRequest<T> request, CancellationToken cancellationToken)
    {
        return Task.FromResult(request.Value);
    }
}
