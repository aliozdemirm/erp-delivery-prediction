namespace App.Application.Contracts.Erp;

public sealed record OrderReadDto(
    string OrderReference,
    DateTimeOffset RequestedDeliveryDateTime,
    string? Priority,
    string? PlanningStatus);

public sealed record OrderItemReadDto(
    string OrderReference,
    string LineReference,
    string ProductReference,
    decimal OrderedQuantity,
    string UnitOfMeasure);

public sealed record OrderSummaryReadDto(
    string OrderReference,
    string ProductReference,
    decimal Quantity,
    DateTimeOffset RequestedDeliveryDateTime);

public sealed record ProductReadDto(
    string ProductReference,
    string? PlanningClassification,
    string UnitOfMeasure);

public sealed record BomItemReadDto(
    string ParentProductReference,
    string ComponentProductReference,
    decimal RequiredQuantityPerParentUnit,
    string UnitOfMeasure,
    string? LineReference);

public sealed record StockLevelReadDto(
    string ProductReference,
    string? LocationReference,
    decimal OnHandQuantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity);

public sealed record OpenPurchaseOrderReadDto(
    string PurchaseOrderReference,
    string ProductReference,
    decimal OpenQuantity,
    DateTimeOffset ExpectedAvailabilityDateTime,
    long? SupplierLeadTimeMinutes,
    string Status);

public sealed record WorkOrderReadDto(
    string WorkOrderReference,
    string? OrderReference,
    string ProductReference,
    string Status,
    RoutingReadDto Routing);

public sealed record RoutingReadDto(
    string RoutingReference,
    IReadOnlyList<OperationReadDto> Operations);

public sealed record OperationReadDto(
    string OperationReference,
    int OperationSequence,
    string WorkCenterReference,
    long StandardDurationMinutes,
    IReadOnlyList<string> PredecessorOperationReferences);

public sealed record CapacityAndCalendarReadDto(
    DateTimeOffset RangeStart,
    DateTimeOffset RangeEnd,
    IReadOnlyList<WorkCenterReadDto> WorkCenters,
    IReadOnlyList<WorkingShiftReadDto> Shifts,
    IReadOnlyList<HolidayReadDto> Holidays,
    IReadOnlyList<PlannedDowntimeReadDto> PlannedDowntimes);

public sealed record WorkCenterReadDto(
    string WorkCenterRef,
    string Name);

public sealed record WorkingShiftReadDto(
    string WorkCenterReference,
    DateTimeOffset Start,
    DateTimeOffset End);

public sealed record HolidayReadDto(
    DateOnly Date,
    string? WorkCenterReference);

public sealed record PlannedDowntimeReadDto(
    string WorkCenterReference,
    DateTimeOffset Start,
    DateTimeOffset End,
    long PlannedDowntimeMinutes);

public sealed record ShippingDurationReadDto(
    string OriginReference,
    string DestinationReference,
    string ShippingProfileReference,
    long ShippingDurationMinutes);
