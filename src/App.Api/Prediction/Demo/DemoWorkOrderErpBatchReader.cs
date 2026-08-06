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
    private const string DemoWorkOrderReference = "DEMO-WO-001";
    private const string DemoRoutingReference = "DEMO-ROUTING-001";
    private const string DemoWorkCenterReference = "DEMO-WC-001";
    private const string DemoWorkOrderStatus = "Released";

    private readonly IErpBatchReader _inner;
    private readonly DemoWorkOrderOptions _options;
    private readonly ILogger<DemoWorkOrderErpBatchReader> _logger;

    public DemoWorkOrderErpBatchReader(
        IErpBatchReader inner,
        DemoWorkOrderOptions options,
        ILogger<DemoWorkOrderErpBatchReader> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ErpBatchSnapshot>> ReadAsync(string orderReference, CancellationToken cancellationToken = default)
    {
        var result = await _inner.ReadAsync(orderReference, cancellationToken);

        if (!_options.EnableSyntheticWorkOrder || !result.IsSuccess)
        {
            return result;
        }

        var snapshot = result.Value;
        if (snapshot.WorkOrders.Count > 0)
        {
            return result;
        }

        var productReference = snapshot.OrderItems.FirstOrDefault()?.ProductReference;
        if (productReference is null)
        {
            return result;
        }

        _logger.LogWarning(
            "Demo mode active: injecting synthetic work order {DemoWorkOrderReference} for order {OrderReference} / product {ProductReference}. " +
            "This routing/operation data is NOT ERP-verified and must not be treated as real production data.",
            DemoWorkOrderReference,
            orderReference,
            productReference);

        var demoWorkOrder = new WorkOrderReadDto(
            DemoWorkOrderReference,
            orderReference,
            productReference,
            DemoWorkOrderStatus,
            new RoutingReadDto(
                DemoRoutingReference,
                new List<OperationReadDto>
                {
                    new("DEMO-OP-10", 10, DemoWorkCenterReference, 60, Array.Empty<string>()),
                    new("DEMO-OP-20", 20, DemoWorkCenterReference, 45, new[] { "DEMO-OP-10" }),
                }));

        var enrichedSnapshot = snapshot with { WorkOrders = new List<WorkOrderReadDto> { demoWorkOrder } };
        return Result<ErpBatchSnapshot>.Success(enrichedSnapshot);
    }
}
