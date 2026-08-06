using App.Application.Contracts.Erp;

namespace App.Application.Abstractions.Erp;

public interface IErpDataProvider
{
    Task<OrderReadDto?> GetOrderAsync(
        string orderReference,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderSummaryReadDto>> GetOrderSummariesAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderItemReadDto>> GetOrderItemsAsync(
        string orderReference,
        CancellationToken cancellationToken);

    Task<ProductReadDto?> GetProductAsync(
        string productReference,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<BomItemReadDto>> GetProductBomAsync(
        string productReference,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<StockLevelReadDto>> GetStockLevelsAsync(
        IReadOnlyList<string> productReferences,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OpenPurchaseOrderReadDto>> GetOpenPurchaseOrdersAsync(
        IReadOnlyList<string> productReferences,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkOrderReadDto>> GetWorkOrdersAsync(
        string orderReference,
        IReadOnlyList<string> productReferences,
        CancellationToken cancellationToken);

    Task<CapacityAndCalendarReadDto> GetCapacityAndCalendarAsync(
        IReadOnlyList<string> workCenterReferences,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd,
        CancellationToken cancellationToken);

    Task<ShippingDurationReadDto?> GetShippingDurationAsync(
        string originReference,
        string destinationReference,
        string shippingProfileReference,
        CancellationToken cancellationToken);
}
