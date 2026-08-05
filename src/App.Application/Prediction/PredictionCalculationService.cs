using App.Application.Abstractions.Erp;
using App.Application.Common;
using App.Application.Contracts.Configuration;
using App.Application.Prediction.Resolvers;
using App.Domain.Abstractions;
using App.Domain.Prediction;

namespace App.Application.Prediction;

public sealed class PredictionCalculationService : IPredictionCalculationService
{
    private readonly IErpBatchReader _erpBatchReader;
    private readonly PredictionContextBuilder _contextBuilder;
    private readonly RuleBasedPredictionEngine _predictionEngine;
    private readonly ICriticalPathCalculator _criticalPathCalculator;
    private readonly IClock _clock;
    private readonly MvpAssumptionsOptions _options;
    private readonly ShippingResolver _shippingResolver;

    public PredictionCalculationService(
        IErpBatchReader erpBatchReader,
        PredictionContextBuilder contextBuilder,
        RuleBasedPredictionEngine predictionEngine,
        ICriticalPathCalculator criticalPathCalculator,
        IClock clock,
        MvpAssumptionsOptions options,
        ShippingResolver shippingResolver)
    {
        _erpBatchReader = erpBatchReader ?? throw new ArgumentNullException(nameof(erpBatchReader));
        _contextBuilder = contextBuilder ?? throw new ArgumentNullException(nameof(contextBuilder));
        _predictionEngine = predictionEngine ?? throw new ArgumentNullException(nameof(predictionEngine));
        _criticalPathCalculator = criticalPathCalculator ?? throw new ArgumentNullException(nameof(criticalPathCalculator));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _shippingResolver = shippingResolver ?? throw new ArgumentNullException(nameof(shippingResolver));
    }

    public async Task<Result<RuleBasedPredictionResult>> CalculateAsync(string orderReference, CancellationToken cancellationToken = default)
    {
        // 1. Read ERP Snapshot
        var snapshotResult = await _erpBatchReader.ReadAsync(orderReference, cancellationToken);
        if (!snapshotResult.IsSuccess)
        {
            return Result<RuleBasedPredictionResult>.Failure(snapshotResult.Error!);
        }

        // 2. Build Context
        var (status, context) = _contextBuilder.Build(snapshotResult.Value!);
        if (status != DataSufficiency.Sufficient || context is null)
        {
            return Result<RuleBasedPredictionResult>.Failure(new Error("Data.Insufficient", "ERP data is insufficient to run prediction.", ErrorType.Validation));
        }

        // 3. Rule Engine
        var engineResult = _predictionEngine.Run(context);
        if (!engineResult.Success)
        {
            return Result<RuleBasedPredictionResult>.Failure(new Error("RuleEngine.Failed", "Order failed rule validation.", ErrorType.Validation));
        }

        // 4. Critical Path Method
        var cpmOutcome = _criticalPathCalculator.Calculate(context);
        if (cpmOutcome.Status != CriticalPathStatus.Success || cpmOutcome.Result is null)
        {
            return Result<RuleBasedPredictionResult>.Failure(new Error("CPM.Failed", $"CPM failed: {cpmOutcome.FailureReason}", ErrorType.Validation));
        }

        // 5. Calendar (Calculate End Date)
        var estimatedStart = _clock.UtcNow;
        var calendar = new WorkingCalendar(_options.WorkingCalendar.MinutesPerDay);
        var estimatedEnd = calendar.AddWorkingMinutes(estimatedStart, cpmOutcome.Result.TotalWorkingMinutes);

        // 6. Shipping (Calculate Delivery Date)
        var shippingResult = _shippingResolver.ResolveShippingDuration(null, _options);
        var estimatedDelivery = estimatedEnd;
        if (shippingResult.Value.HasValue)
        {
            estimatedDelivery = estimatedDelivery.Add(shippingResult.Value.Value);
        }

        var fallbackReasons = new List<string>(engineResult.AppliedFallbackReasons);
        if (shippingResult.IsFallbackApplied && shippingResult.Reason != null)
        {
            fallbackReasons.Add(shippingResult.Reason);
        }

        // Map Timeline
        var timeline = cpmOutcome.Result.Schedule.Select(s => {
            var opStart = calendar.AddWorkingMinutes(estimatedStart, s.EarliestStartMinutes);
            var opEnd = calendar.AddWorkingMinutes(estimatedStart, s.EarliestFinishMinutes);
            var isCritical = cpmOutcome.Result.CriticalOperationRefs.Contains(s.OperationRef);
            return new TimelineItem(s.OperationRef, opStart, opEnd, isCritical);
        }).ToList();

        // 7. Map Result
        var predictionResult = new RuleBasedPredictionResult(
            orderReference,
            estimatedStart,
            estimatedEnd,
            estimatedDelivery,
            cpmOutcome.Result.CriticalOperationRefs,
            fallbackReasons,
            engineResult.MaterialShortages,
            timeline
        );

        return Result<RuleBasedPredictionResult>.Success(predictionResult);
    }
}
