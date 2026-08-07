using App.Application.Contracts.Erp;
using App.Application.Prediction;
using App.Application.Prediction.Demo;
using App.Domain.Prediction;

namespace App.Api.Prediction.Demo;

/// <summary>
/// Applies the existing T-388 fallback before context data sufficiency is evaluated. This lets
/// callers such as What-If reuse the same demo routing without changing their snapshot builder.
/// </summary>
public sealed class DemoWorkOrderPredictionContextBuilder : IPredictionContextBuilder
{
    private readonly IPredictionContextBuilder _inner;
    private readonly DemoWorkOrderSnapshotEnricher _enricher;
    private readonly ILogger<DemoWorkOrderPredictionContextBuilder> _logger;

    public DemoWorkOrderPredictionContextBuilder(
        IPredictionContextBuilder inner,
        DemoWorkOrderSnapshotEnricher enricher,
        ILogger<DemoWorkOrderPredictionContextBuilder> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _enricher = enricher ?? throw new ArgumentNullException(nameof(enricher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public (DataSufficiency Status, PredictionContext? Context) Build(ErpBatchSnapshot snapshot)
    {
        var enrichedSnapshot = _enricher.Enrich(snapshot);
        if (!ReferenceEquals(enrichedSnapshot, snapshot))
        {
            var workOrder = enrichedSnapshot.WorkOrders.Single();
            _logger.LogWarning(
                "Demo mode active: injecting synthetic work order {DemoWorkOrderReference} for order {OrderReference} / product {ProductReference}. " +
                "This routing/operation data is NOT ERP-verified and must not be treated as real production data.",
                workOrder.WorkOrderReference,
                workOrder.OrderReference,
                workOrder.ProductReference);
        }

        return _inner.Build(enrichedSnapshot);
    }
}
