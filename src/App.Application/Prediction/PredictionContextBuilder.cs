using App.Application.Contracts.Erp;
using App.Domain.Prediction;

namespace App.Application.Prediction;

public sealed class PredictionContextBuilder : IPredictionContextBuilder
{
    public (DataSufficiency Status, PredictionContext? Context) Build(ErpBatchSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        // 1. Check Quantity
        var orderItem = snapshot.OrderItems.FirstOrDefault();
        if (orderItem is null || orderItem.OrderedQuantity <= 0)
        {
            return (DataSufficiency.InsufficientData, null);
        }

        // 2. Check BOM
        if (snapshot.BomItems.Count == 0)
        {
            return (DataSufficiency.InsufficientData, null);
        }

        // 3. Check Routing (from WorkOrders)
        var hasRouting = snapshot.WorkOrders.Any(wo => wo.Routing != null && wo.Routing.Operations.Count > 0);
        if (!hasRouting)
        {
            return (DataSufficiency.InsufficientData, null);
        }

        // Map OrderInput
        var orderInput = new OrderInput(
            snapshot.Order.OrderReference,
            orderItem.ProductReference,
            orderItem.OrderedQuantity,
            snapshot.Order.RequestedDeliveryDateTime);

        // Map MaterialSnapshot
        var products = snapshot.Products
            .Select(p => new MaterialProduct(p.ProductReference, p.UnitOfMeasure))
            .ToList();

        var bomItems = snapshot.BomItems
            .Select(b => new MaterialBomItem(b.ParentProductReference, b.ComponentProductReference, b.RequiredQuantityPerParentUnit))
            .ToList();

        var stockLevels = snapshot.StockLevels
            .Select(s => new MaterialStock(s.ProductReference, s.OnHandQuantity))
            .ToList();

        var openPos = snapshot.OpenPurchaseOrders
            .Select(po => new MaterialPurchaseOrder(po.PurchaseOrderReference, po.ProductReference, po.OpenQuantity, po.ExpectedAvailabilityDateTime))
            .ToList();

        var materialSnapshot = new MaterialSnapshot(products, bomItems, stockLevels, openPos);

        // Map RoutingSnapshot
        var routingOperations = snapshot.WorkOrders
            .Where(wo => wo.Routing != null)
            .SelectMany(wo => wo.Routing!.Operations)
            .Select(op => new RoutingOperation(
                op.OperationReference,
                op.OperationSequence,
                op.WorkCenterReference,
                op.StandardDurationMinutes,
                op.PredecessorOperationReferences))
            .ToList();

        var routingSnapshot = new RoutingSnapshot(routingOperations);

        // Empty Snapshots for Capacity, Calendar, Shipping (T-365 out of scope)
        var capacitySnapshot = new CapacitySnapshot();
        var calendarSnapshot = new CalendarSnapshot();
        var shippingSnapshot = new ShippingSnapshot();

        var context = new PredictionContext(
            orderInput,
            materialSnapshot,
            routingSnapshot,
            capacitySnapshot,
            calendarSnapshot,
            shippingSnapshot);

        return (DataSufficiency.Sufficient, context);
    }
}
