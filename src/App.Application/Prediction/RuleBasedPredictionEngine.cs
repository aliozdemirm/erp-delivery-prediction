using App.Application.Contracts.Configuration;
using App.Application.Prediction.Resolvers;
using App.Application.Prediction.Steps;
using App.Domain.Abstractions;
using App.Domain.Prediction;

namespace App.Application.Prediction;

/// <summary>
/// Runs the fixed six-step Rule-Based pipeline over a PredictionContext (SAD §11, scoped to
/// T-369): ValidateOrder, ExpandBom, CheckStock, ResolveProcurement, CalculateRoutingDurations,
/// ApplyCapacity. CPM, Working Calendar and Shipping are out of scope and run in later tasks.
/// </summary>
public sealed class RuleBasedPredictionEngine
{
    private readonly ProcurementResolver _procurementResolver;
    private readonly CapacityResolver _capacityResolver;
    private readonly IClock _clock;
    private readonly MvpAssumptionsOptions _options;

    public RuleBasedPredictionEngine(
        ProcurementResolver procurementResolver,
        CapacityResolver capacityResolver,
        IClock clock,
        MvpAssumptionsOptions options)
    {
        ArgumentNullException.ThrowIfNull(procurementResolver);
        ArgumentNullException.ThrowIfNull(capacityResolver);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(options);

        _procurementResolver = procurementResolver;
        _capacityResolver = capacityResolver;
        _clock = clock;
        _options = options;
    }

    public EngineResult Run(PredictionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var validateOrderStep = new ValidateOrderStep();
        validateOrderStep.Execute(context);

        if (!validateOrderStep.IsValid)
        {
            return new EngineResult(false, context, Array.Empty<MaterialShortage>(), Array.Empty<string>());
        }

        var expandBomStep = new ExpandBomStep();
        expandBomStep.Execute(context);

        var checkStockStep = new CheckStockStep(expandBomStep.ExpandedBomItems);
        checkStockStep.Execute(context);

        var resolveProcurementStep = new ResolveProcurementStep(checkStockStep.Shortages, _procurementResolver, _clock.UtcNow, _options);
        resolveProcurementStep.Execute(context);

        var calculateRoutingDurationsStep = new CalculateRoutingDurationsStep();
        calculateRoutingDurationsStep.Execute(context);

        var applyCapacityStep = new ApplyCapacityStep(_capacityResolver);
        applyCapacityStep.Execute(context);

        var appliedFallbackReasons = new List<string>(resolveProcurementStep.AppliedFallbackReasons);
        if (applyCapacityStep.AppliedFallbackReason is not null)
        {
            appliedFallbackReasons.Add(applyCapacityStep.AppliedFallbackReason);
        }

        return new EngineResult(true, context, checkStockStep.Shortages, appliedFallbackReasons);
    }
}
