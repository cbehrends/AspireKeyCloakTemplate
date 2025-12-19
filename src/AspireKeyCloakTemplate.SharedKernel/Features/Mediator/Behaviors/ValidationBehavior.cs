using System.Diagnostics;
using System.Diagnostics.Metrics;
using FluentValidation;

namespace AspireKeyCloakTemplate.SharedKernel.Features.Mediator.Behaviors;

/// <summary>
///     OpenTelemetry metrics for validation tracking
/// </summary>
internal static class ValidationMetrics
{
    private static readonly Meter Meter = new("AspireKeyCloakTemplate.Mediator", "1.0.0");

    public static readonly Counter<long> ValidatorExecutionCounter = Meter.CreateCounter<long>(
        "mediator.validation.executions",
        "{execution}",
        "Total number of validation executions");

    public static readonly Counter<long> ValidatorRunCounter = Meter.CreateCounter<long>(
        "mediator.validation.validators_run",
        "{validator}",
        "Total number of individual validators run");

    public static readonly Histogram<double> ValidationDuration = Meter.CreateHistogram<double>(
        "mediator.validation.duration",
        "ms",
        "Duration of validation execution");

    public static readonly Counter<long> ValidationFailureCounter = Meter.CreateCounter<long>(
        "mediator.validation.failures",
        "{failure}",
        "Total number of validation failures");
}

/// <summary>
///     Pipeline behavior that validates requests using FluentValidation validators
/// </summary>
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var validatorsList = validators.ToList();
        var validatorCount = validatorsList.Count;

        if (validatorCount == 0) return await next();

        var stopwatch = Stopwatch.StartNew();

        // Track validation execution and number of validators
        ValidationMetrics.ValidatorExecutionCounter.Add(1,
            new KeyValuePair<string, object?>("request.name", requestName));

        ValidationMetrics.ValidatorRunCounter.Add(validatorCount,
            new KeyValuePair<string, object?>("request.name", requestName));

        try
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                validatorsList.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            stopwatch.Stop();

            if (failures.Count != 0)
            {
                // Record validation failure
                ValidationMetrics.ValidationFailureCounter.Add(1,
                    new KeyValuePair<string, object?>("request.name", requestName),
                    new KeyValuePair<string, object?>("failure.count", failures.Count));

                ValidationMetrics.ValidationDuration.Record(stopwatch.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("request.name", requestName),
                    new KeyValuePair<string, object?>("validator.count", validatorCount),
                    new KeyValuePair<string, object?>("status", "failed"));

                throw new ValidationException(failures);
            }

            // Record successful validation duration
            ValidationMetrics.ValidationDuration.Record(stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("request.name", requestName),
                new KeyValuePair<string, object?>("validator.count", validatorCount),
                new KeyValuePair<string, object?>("status", "success"));

            return await next();
        }
        catch (ValidationException)
        {
            // Already tracked above, just rethrow
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Track unexpected errors during validation
            ValidationMetrics.ValidationDuration.Record(stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("request.name", requestName),
                new KeyValuePair<string, object?>("validator.count", validatorCount),
                new KeyValuePair<string, object?>("status", "error"),
                new KeyValuePair<string, object?>("exception.type", ex.GetType().Name));

            throw;
        }
    }
}
