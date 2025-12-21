using AspireKeyCloakTemplate.SharedKernel.Features.Mediator;

namespace AspireKeyCloakTemplate.SharedKernel.UnitTests.Helpers;

public record TestRequest(string Value) : IRequest<TestResponse>;

public record TestResponse(string Result);

public record TestVoidRequest(string Value) : IRequest;

public record GenericRequest<T>(T Value) : IRequest<T>;
