using App.Application.Contracts.Configuration;
using App.Application.Prediction.Resolvers;
using App.Domain.Prediction;

namespace App.Application.Prediction.Steps;

/// <summary>
/// Resolves an availability date for each shortage found by CheckStockStep, delegating to
/// the T-367 ProcurementResolver (open PO date, or MVP fallback lead time). Does not
/// reimplement fallback logic.
/// </summary>
public sealed class ResolveProcurementStep : IPredictionStep
{
    private readonly IReadOnlyList<MaterialShortage> _shortages;
    private readonly ProcurementResolver _resolver;
    private readonly DateTimeOffset _currentTime;
    private readonly MvpAssumptionsOptions _options;

    public IReadOnlyList<string> AppliedFallbackReasons { get; private set; } = Array.Empty<string>();

    public ResolveProcurementStep(
        IReadOnlyList<MaterialShortage> shortages,
        ProcurementResolver resolver,
        DateTimeOffset currentTime,
        MvpAssumptionsOptions options)
    {
        ArgumentNullException.ThrowIfNull(shortages);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(options);

        _shortages = shortages;
        _resolver = resolver;
        _currentTime = currentTime;
        _options = options;
    }

    public void Execute(PredictionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var reasons = new List<string>();

        foreach (var shortage in _shortages)
        {
            var openPo = context.MaterialSnapshot.OpenPurchaseOrders
                .FirstOrDefault(po => po.ProductReference == shortage.ProductReference);

            var result = _resolver.ResolveAvailabilityDate(openPo, _currentTime, _options);

            if (result.IsFallbackApplied && result.Reason is not null)
            {
                reasons.Add(result.Reason);
            }
        }

        AppliedFallbackReasons = reasons;
    }
}
