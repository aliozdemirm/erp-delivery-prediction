using App.Application.Abstractions.Erp;
using App.Application.IntegrationLogging;
using App.Integration.MockErp;
using Microsoft.AspNetCore.Mvc.Testing;

namespace App.Integration.Tests.MockErp;

public sealed class RuntimeSeedProviderIntegrationTests : IClassFixture<WebApplicationFactory<global::MockErp.Api.Controllers.OrdersController>>
{
    private readonly IErpDataProvider _provider;

    public RuntimeSeedProviderIntegrationTests(WebApplicationFactory<global::MockErp.Api.Controllers.OrdersController> factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        _provider = new MockErpDataProvider(client, new TestIntegrationLogWriter());
    }

    [Fact]
    public async Task RealRuntimePipelineReadsCoreFullSeedData()
    {
        var order = await _provider.GetOrderAsync("SO00001", default);

        Assert.NotNull(order);
        Assert.Equal("SO00001", order.OrderReference);
        Assert.Equal(
            new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero),
            order.RequestedDeliveryDateTime);

        var orderItem = Assert.Single(await _provider.GetOrderItemsAsync("SO00001", default));
        Assert.Equal("SO00001", orderItem.OrderReference);
        Assert.Equal("P002", orderItem.ProductReference);
        Assert.Equal(16m, orderItem.OrderedQuantity);
        Assert.Equal("Adet", orderItem.UnitOfMeasure);

        var product = await _provider.GetProductAsync("P002", default);
        Assert.NotNull(product);
        Assert.Equal("P002", product.ProductReference);
        Assert.Equal("Adet", product.UnitOfMeasure);

        var products = await _provider.GetProductsAsync(default);
        Assert.Equal(4, products.Count);
        Assert.Equal(["P001", "P002", "P003", "P004"], products.Select(p => p.ProductReference).Order(StringComparer.Ordinal));
        Assert.All(products, p => Assert.Null(p.PlanningClassification));

        var bom = await _provider.GetProductBomAsync("P002", default);
        Assert.Equal(11, bom.Count);
        Assert.Contains(bom, line => line.ComponentProductReference == "MAT-AHSAP-OTURAK");

        var stock = Assert.Single(await _provider.GetStockLevelsAsync(["P002"], default));
        Assert.Equal(500m, stock.OnHandQuantity);
        Assert.Equal(0m, stock.ReservedQuantity);
        Assert.Equal(500m, stock.AvailableQuantity);
        Assert.Null(stock.LocationReference);
    }

    [Fact]
    public async Task RealRuntimePipelinePreservesIntentionallyEmptySections()
    {
        var purchaseOrders = await _provider.GetOpenPurchaseOrdersAsync(["P002"], default);
        Assert.NotNull(purchaseOrders);
        Assert.Empty(purchaseOrders);

        var workOrders = await _provider.GetWorkOrdersAsync("SO00001", ["P002"], default);
        Assert.NotNull(workOrders);
        Assert.Empty(workOrders);

        var rangeStart = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
        var rangeEnd = new DateTimeOffset(2026, 7, 31, 23, 59, 59, TimeSpan.Zero);
        var capacity = await _provider.GetCapacityAndCalendarAsync(
            ["WC-ASSEMBLY-01"],
            rangeStart,
            rangeEnd,
            default);

        Assert.Equal(rangeStart, capacity.RangeStart);
        Assert.Equal(rangeEnd, capacity.RangeEnd);
        Assert.NotNull(capacity.WorkCenters);
        Assert.Empty(capacity.WorkCenters);
        Assert.NotNull(capacity.Shifts);
        Assert.Empty(capacity.Shifts);
        Assert.NotNull(capacity.Holidays);
        Assert.Empty(capacity.Holidays);
        Assert.NotNull(capacity.PlannedDowntimes);
        Assert.Empty(capacity.PlannedDowntimes);

        var shipping = await _provider.GetShippingDurationAsync(
            "WH-IST-01",
            "CUSTOMER-ANK-01",
            "STANDARD",
            default);
        Assert.Null(shipping);
    }

    private sealed class TestIntegrationLogWriter : IIntegrationLogWriter
    {
        public Task WriteAsync(
            IntegrationLogRequest request,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
