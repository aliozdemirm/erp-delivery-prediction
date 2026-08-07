using App.Application.Contracts.Erp;

namespace App.Application.Prediction.Demo;

/// <summary>
/// The single source of T-388 synthetic work-order data. Enriches only snapshots that do not
/// already contain ERP work orders.
/// </summary>
public sealed class DemoWorkOrderSnapshotEnricher
{
    public const string WorkOrderReference = "DEMO-WO-001";

    public ErpBatchSnapshot Enrich(ErpBatchSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.WorkOrders.Count > 0)
        {
            return snapshot;
        }

        var productReference = snapshot.OrderItems.FirstOrDefault()?.ProductReference;
        if (productReference is null)
        {
            return snapshot;
        }

        var demoWorkOrder = new WorkOrderReadDto(
            WorkOrderReference,
            snapshot.Order.OrderReference,
            productReference,
            "Released",
            new RoutingReadDto(
                "DEMO-ROUTING-001",
                new List<OperationReadDto>
                {
                    new("DEMO-OP-10", 10, "DEMO-WC-001", 60, Array.Empty<string>()),
                    new("DEMO-OP-20", 20, "DEMO-WC-001", 45, new[] { "DEMO-OP-10" }),
                }));

        return snapshot with { WorkOrders = new List<WorkOrderReadDto> { demoWorkOrder } };
    }
}
