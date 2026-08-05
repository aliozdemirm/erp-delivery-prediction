using App.Application.Contracts.Configuration;
using App.Application.Contracts.Erp;
using App.Application.Prediction.Resolvers;
using App.Domain.Prediction;

namespace App.Application.Prediction;

public sealed class PredictionDataReadinessService
{
    private const string InsufficientPredictionContextReason = "Prediction context data is insufficient.";

    private readonly IPredictionContextBuilder _contextBuilder;
    private readonly IProcurementResolver _procurementResolver;
    private readonly IShippingResolver _shippingResolver;
    private readonly ICapacityResolver _capacityResolver;

    public PredictionDataReadinessService(
        IPredictionContextBuilder contextBuilder,
        IProcurementResolver procurementResolver,
        IShippingResolver shippingResolver,
        ICapacityResolver capacityResolver)
    {
        ArgumentNullException.ThrowIfNull(contextBuilder);
        ArgumentNullException.ThrowIfNull(procurementResolver);
        ArgumentNullException.ThrowIfNull(shippingResolver);
        ArgumentNullException.ThrowIfNull(capacityResolver);

        _contextBuilder = contextBuilder;
        _procurementResolver = procurementResolver;
        _shippingResolver = shippingResolver;
        _capacityResolver = capacityResolver;
    }

    public ReadinessResult Evaluate(
        ErpBatchSnapshot snapshot,
        MaterialPurchaseOrder? openPurchaseOrder,
        long? actualShippingDurationMinutes,
        DateTimeOffset currentTime,
        MvpAssumptionsOptions options)
    {
        var (status, context) = _contextBuilder.Build(snapshot);
        if (status == DataSufficiency.InsufficientData || context is null)
        {
            return new InsufficientData(
                ReadinessFailureSource.PredictionContext,
                InsufficientPredictionContextReason);
        }

        var procurement = _procurementResolver.ResolveAvailabilityDate(openPurchaseOrder, currentTime, options);
        var shipping = _shippingResolver.ResolveShippingDuration(actualShippingDurationMinutes, options);
        if (shipping.Value is null)
        {
            return new InsufficientData(ReadinessFailureSource.Shipping, shipping.Reason);
        }

        var capacity = _capacityResolver.ResolveCapacityConstraint();
        return new Ready(context, procurement, shipping, capacity);
    }
}
