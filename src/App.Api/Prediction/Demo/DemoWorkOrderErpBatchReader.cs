using App.Application.Abstractions.Erp;
using App.Application.Common;
using App.Application.Contracts.Erp;
using App.Application.Prediction.Demo;
using Microsoft.Extensions.Logging;

namespace App.Api.Prediction.Demo;

/// <summary>
/// Opt-in decorator (see DemoWorkOrderOptions.EnableSyntheticWorkOrder) that injects a single,
/// clearly-labelled DEMO-* work order/routing when the real ERP snapshot has none. Never
/// overrides a snapshot that already contains real WorkOrders. Not ERP-verified data — see
/// T-388. Lives in App.Api (composition root) because it is a concrete implementation, per the
/// repository convention that concrete implementations are registered only in App.Api.
/// </summary>
public sealed class DemoWorkOrderErpBatchReader : IErpBatchReader
{
    private readonly IErpBatchReader _inner;
    private readonly DemoWorkOrderOptions _options;
    private readonly DemoWorkOrderSnapshotEnricher _enricher;
    private readonly ILogger<DemoWorkOrderErpBatchReader> _logger;

    public DemoWorkOrderErpBatchReader(
        IErpBatchReader inner,
        DemoWorkOrderOptions options,
        DemoWorkOrderSnapshotEnricher enricher,
        ILogger<DemoWorkOrderErpBatchReader> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _enricher = enricher ?? throw new ArgumentNullException(nameof(enricher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ErpBatchSnapshot>> ReadAsync(string orderReference, CancellationToken cancellationToken = default)
    {
        var result = await _inner.ReadAsync(orderReference, cancellationToken);

        if (!_options.EnableSyntheticWorkOrder || !result.IsSuccess)
        {
            return result;
        }

        var enrichedSnapshot = _enricher.Enrich(result.Value);
        if (ReferenceEquals(enrichedSnapshot, result.Value))
        {
            return result;
        }

        var demoWorkOrder = enrichedSnapshot.WorkOrders.Single();

        _logger.LogWarning(
            "Demo mode active: injecting synthetic work order {DemoWorkOrderReference} for order {OrderReference} / product {ProductReference}. " +
            "This routing/operation data is NOT ERP-verified and must not be treated as real production data.",
            demoWorkOrder.WorkOrderReference,
            orderReference,
            demoWorkOrder.ProductReference);

        return Result<ErpBatchSnapshot>.Success(enrichedSnapshot);
    }
}
