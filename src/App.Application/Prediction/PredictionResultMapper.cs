using App.Application.Common;
using App.Application.Contracts.Configuration;
using App.Application.Prediction.Resolvers;
using App.Domain.Abstractions;
using App.Domain.Prediction;

namespace App.Application.Prediction;

public sealed class PredictionResultMapper
{
    private readonly IClock _clock;
    private readonly MvpAssumptionsOptions _options;
    private readonly ShippingResolver _shippingResolver;

    public PredictionResultMapper(
        IClock clock,
        MvpAssumptionsOptions options,
        ShippingResolver shippingResolver)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _shippingResolver = shippingResolver ?? throw new ArgumentNullException(nameof(shippingResolver));
    }

    public Result<RuleBasedPredictionResult> Map(
        string orderReference,
        EngineResult engineResult,
        CriticalPathOutcome criticalPathOutcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderReference);
        ArgumentNullException.ThrowIfNull(engineResult);
        ArgumentNullException.ThrowIfNull(criticalPathOutcome);

        if (criticalPathOutcome.Status == CriticalPathStatus.Success && criticalPathOutcome.Result is null)
        {
            throw new InvalidOperationException("A successful critical path outcome must contain a result.");
        }

        if (criticalPathOutcome.Status != CriticalPathStatus.Success)
        {
            var code = criticalPathOutcome.Status switch
            {
                CriticalPathStatus.CycleDetected => "CPM.CycleDetected",
                CriticalPathStatus.MissingPredecessorReference => "CPM.MissingPredecessorReference",
                _ => throw new InvalidOperationException(
                    $"Unsupported critical path status: {criticalPathOutcome.Status}.")
            };

            return Result<RuleBasedPredictionResult>.Failure(
                new Error(
                    code,
                    $"CPM failed: {criticalPathOutcome.FailureReason}",
                    ErrorType.Validation));
        }

        var cpmResult = criticalPathOutcome.Result!;
        var estimatedStart = _clock.UtcNow;
        var calendar = new WorkingCalendar(_options.WorkingCalendar.MinutesPerDay);
        var estimatedEnd = calendar.AddWorkingMinutes(estimatedStart, cpmResult.TotalWorkingMinutes);

        var shippingResult = _shippingResolver.ResolveShippingDuration(null, _options);
        var estimatedDelivery = shippingResult.Value.HasValue
            ? estimatedEnd.Add(shippingResult.Value.Value)
            : estimatedEnd;

        var fallbackReasons = new List<string>(engineResult.AppliedFallbackReasons);
        if (shippingResult.IsFallbackApplied && shippingResult.Reason is not null)
        {
            fallbackReasons.Add(shippingResult.Reason);
        }

        var timeline = cpmResult.Schedule
            .Select(schedule => new TimelineItem(
                schedule.OperationRef,
                calendar.AddWorkingMinutes(estimatedStart, schedule.EarliestStartMinutes),
                calendar.AddWorkingMinutes(estimatedStart, schedule.EarliestFinishMinutes),
                cpmResult.CriticalOperationRefs.Contains(schedule.OperationRef)))
            .ToList();

        return Result<RuleBasedPredictionResult>.Success(
            new RuleBasedPredictionResult(
                orderReference,
                estimatedStart,
                estimatedEnd,
                estimatedDelivery,
                cpmResult.CriticalOperationRefs,
                fallbackReasons,
                engineResult.MaterialShortages,
                timeline));
    }
}
