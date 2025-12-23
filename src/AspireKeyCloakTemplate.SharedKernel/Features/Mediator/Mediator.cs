using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace AspireKeyCloakTemplate.SharedKernel.Features.Mediator;

/// <summary>
///     Default mediator implementation
/// </summary>
public class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private static readonly ConcurrentDictionary<Type, object> HandlerWrapperCache = new();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        
        var wrapper = (RequestHandlerWrapperBase<TResponse>)HandlerWrapperCache.GetOrAdd(
            requestType,
            static rt =>
            {
                var responseType = typeof(TResponse);
                var handlerWrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(rt, responseType);
                return Activator.CreateInstance(handlerWrapperType)!;
            });

        return wrapper.Handle(request, serviceProvider, cancellationToken);
    }

    private abstract class RequestHandlerWrapperBase<TResponse>
    {
        public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider,
            CancellationToken cancellationToken);
    }

    private sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerWrapperBase<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public override Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
        {
            Task<TResponse> Handler()
            {
                return GetHandler(serviceProvider).Handle((TRequest)request, cancellationToken);
            }

            var behaviors = serviceProvider.GetService<IEnumerable<IPipelineBehavior<TRequest, TResponse>>>();

            if (behaviors == null) return Handler();
            
            // Use array and manual iteration for better performance
            var behaviorArray = behaviors as IPipelineBehavior<TRequest, TResponse>[] ?? behaviors.ToArray();
            if (behaviorArray.Length == 0) return Handler();

            RequestHandlerDelegate<TResponse> next = Handler;
            
            // Build pipeline in reverse order without LINQ
            for (var i = behaviorArray.Length - 1; i >= 0; i--)
            {
                var behavior = behaviorArray[i];
                var currentNext = next;
                next = () => behavior.Handle((TRequest)request, currentNext, cancellationToken);
            }
            
            return next();
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
