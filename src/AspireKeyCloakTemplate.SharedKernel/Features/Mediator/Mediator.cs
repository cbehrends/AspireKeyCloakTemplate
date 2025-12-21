using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace AspireKeyCloakTemplate.SharedKernel.Features.Mediator;

/// <summary>
///     Default mediator implementation
/// </summary>
public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var responseType = typeof(TResponse);

        var handlerWrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, responseType);
        var handlerWrapper = Activator.CreateInstance(handlerWrapperType);

        var handleMethod =
            handlerWrapperType.GetMethod(nameof(RequestHandlerWrapper<IRequest<TResponse>, TResponse>.Handle));

        try
        {
            return (Task<TResponse>)handleMethod!.Invoke(handlerWrapper,
                [request, _serviceProvider, cancellationToken])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            // Unwrap the TargetInvocationException to preserve the original exception
            throw ex.InnerException;
        }
    }

    private sealed class RequestHandlerWrapper<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
        {
            Task<TResponse> Handler()
            {
                return GetHandler(serviceProvider).Handle((TRequest)request, cancellationToken);
            }

            var behaviors = serviceProvider.GetService<IEnumerable<IPipelineBehavior<TRequest, TResponse>>>();

            if (behaviors == null || !behaviors.Any()) return Handler();

            return behaviors
                .Reverse()
                .Aggregate(
                    (RequestHandlerDelegate<TResponse>)Handler,
                    (next, behavior) => () => behavior.Handle((TRequest)request, next, cancellationToken))();
        }

        private static IRequestHandler<TRequest, TResponse> GetHandler(IServiceProvider serviceProvider)
        {
            var handler = serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>();

            if (handler == null)
                throw new InvalidOperationException($"Handler not found for request type {typeof(TRequest).Name}");

            return handler;
        }
    }
}
