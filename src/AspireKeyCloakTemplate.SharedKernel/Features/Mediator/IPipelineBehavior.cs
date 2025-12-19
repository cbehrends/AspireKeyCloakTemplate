namespace AspireKeyCloakTemplate.SharedKernel.Features.Mediator;

/// <summary>
///     Pipeline behavior to surround the inner handler
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    ///     Pipeline handler. Perform any additional behavior and await the next delegate as necessary
    /// </summary>
    /// <param name="request">Incoming request</param>
    /// <param name="next">Awaitable delegate for the next action in the pipeline</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Awaitable task returning the response</returns>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}

/// <summary>
///     Represents an async continuation for the next task to execute in the pipeline
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
/// <returns>Awaitable task returning the response</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
