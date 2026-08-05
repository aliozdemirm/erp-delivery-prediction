using App.Application.Prediction.Resolvers;
using App.Domain.Prediction;

namespace App.Application.Prediction.Steps;

/// <summary>
/// Applies the T-367 CapacityResolver's constraint result. The resolver currently always
/// returns an unlimited-capacity fallback (CapacitySnapshot carries no real data yet), so
/// this step's only job is calling it and surfacing the fallback reason.
/// </summary>
public sealed class ApplyCapacityStep : IPredictionStep
{
    private readonly CapacityResolver _resolver;

    public string? AppliedFallbackReason { get; private set; }

    public ApplyCapacityStep(CapacityResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolver = resolver;
    }

    public void Execute(PredictionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var result = _resolver.ResolveCapacityConstraint();
        AppliedFallbackReason = result.IsFallbackApplied ? result.Reason : null;
    }
}
