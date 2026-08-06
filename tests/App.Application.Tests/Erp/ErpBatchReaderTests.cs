using App.Application.Abstractions.Erp;
using App.Application.Contracts.Erp;
using App.Application.Erp;
using App.Domain.Abstractions;

namespace App.Application.Tests.Erp;

public class ErpBatchReaderTests
{
    private class FakeClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }

    private class FakeErpDataProvider : IErpDataProvider
    {
        public OrderReadDto? OrderToReturn { get; set; }
        public List<OrderItemReadDto>? OrderItemsToReturn { get; set; }
        public Dictionary<string, ProductReadDto> ProductsToReturn { get; set; } = new();
        public Dictionary<string, List<BomItemReadDto>> BomItemsToReturn { get; set; } = new();
        public List<StockLevelReadDto>? StockLevelsToReturn { get; set; }
        public List<OpenPurchaseOrderReadDto>? OpenPurchaseOrdersToReturn { get; set; }
        public List<WorkOrderReadDto>? WorkOrdersToReturn { get; set; }

        public Task<OrderReadDto?> GetOrderAsync(string orderReference, CancellationToken cancellationToken)
            => Task.FromResult(OrderToReturn);

        public Task<IReadOnlyList<OrderSummaryReadDto>> GetOrderSummariesAsync(CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<OrderItemReadDto>> GetOrderItemsAsync(string orderReference, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<OrderItemReadDto>>(OrderItemsToReturn ?? new List<OrderItemReadDto>());

        public Task<ProductReadDto?> GetProductAsync(string productReference, CancellationToken cancellationToken)
        {
            ProductsToReturn.TryGetValue(productReference, out var product);
            return Task.FromResult(product);
        }

        public Task<IReadOnlyList<BomItemReadDto>> GetProductBomAsync(string productReference, CancellationToken cancellationToken)
        {
            BomItemsToReturn.TryGetValue(productReference, out var bom);
            return Task.FromResult<IReadOnlyList<BomItemReadDto>>(bom ?? new List<BomItemReadDto>());
        }

        public Task<IReadOnlyList<StockLevelReadDto>> GetStockLevelsAsync(IReadOnlyList<string> productReferences, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<StockLevelReadDto>>(StockLevelsToReturn ?? new List<StockLevelReadDto>());

        public Task<IReadOnlyList<OpenPurchaseOrderReadDto>> GetOpenPurchaseOrdersAsync(IReadOnlyList<string> productReferences, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<OpenPurchaseOrderReadDto>>(OpenPurchaseOrdersToReturn ?? new List<OpenPurchaseOrderReadDto>());

        public Task<IReadOnlyList<WorkOrderReadDto>> GetWorkOrdersAsync(string orderReference, IReadOnlyList<string> productReferences, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<WorkOrderReadDto>>(WorkOrdersToReturn ?? new List<WorkOrderReadDto>());

        public Task<CapacityAndCalendarReadDto> GetCapacityAndCalendarAsync(IReadOnlyList<string> workCenterReferences, DateTimeOffset rangeStart, DateTimeOffset rangeEnd, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ShippingDurationReadDto?> GetShippingDurationAsync(string originReference, string destinationReference, string shippingProfileReference, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }

    [Fact]
    public async Task ReadAsync_OrderNotFound_ReturnsFailure()
    {
        var provider = new FakeErpDataProvider { OrderToReturn = null };
        var reader = new ErpBatchReader(provider, new FakeClock());

        var result = await reader.ReadAsync("ORD-1");

        Assert.True(result.IsFailure);
        Assert.Equal("ErpBatchReader.OrderNotFound", result.Error?.Code);
    }

    [Fact]
    public async Task ReadAsync_OrderFound_CollectsAllData_AndToleratesMissingData()
    {
        var provider = new FakeErpDataProvider
        {
            OrderToReturn = new OrderReadDto("ORD-1", DateTimeOffset.UtcNow, null, null),
            OrderItemsToReturn = new List<OrderItemReadDto>
            {
                new OrderItemReadDto("ORD-1", "L1", "PROD-1", 10, "EA")
            },
            ProductsToReturn = new Dictionary<string, ProductReadDto>
            {
                { "PROD-1", new ProductReadDto("PROD-1", null, "EA") }
            }
            // Bom, Stock, POs, WorkOrders are left null to simulate missing/empty data
        };

        var clock = new FakeClock();
        var reader = new ErpBatchReader(provider, clock);

        var result = await reader.ReadAsync("ORD-1");

        Assert.True(result.IsSuccess);
        var snapshot = result.Value;

        Assert.Equal(clock.UtcNow, snapshot.ReadAtUtc);
        Assert.Equal("ORD-1", snapshot.Order.OrderReference);
        Assert.Single(snapshot.OrderItems);
        Assert.Single(snapshot.Products);
        
        // Null collections normalized to empty
        Assert.Empty(snapshot.BomItems);
        Assert.Empty(snapshot.StockLevels);
        Assert.Empty(snapshot.OpenPurchaseOrders);
        Assert.Empty(snapshot.WorkOrders);
    }
}
