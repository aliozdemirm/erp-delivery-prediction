using App.Application.Abstractions.Erp;
using App.Application.Erp;
using App.Application.IntegrationLogging;
using App.Domain.Abstractions;
using App.Integration.MockErp;
using Microsoft.AspNetCore.Mvc.Testing;

namespace App.Integration.Tests.MockErp;

public sealed class ErpBatchReaderIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IErpBatchReader _batchReader;

    private class IntegrationFakeClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new DateTimeOffset(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class TestIntegrationLogWriter : IIntegrationLogWriter
    {
        public Task WriteAsync(IntegrationLogRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public ErpBatchReaderIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var provider = new MockErpDataProvider(client, new TestIntegrationLogWriter());
        _batchReader = new ErpBatchReader(provider, new IntegrationFakeClock());
    }

    [Fact]
    public async Task ReadAsync_WithRealSeedData_ProducesCorrectSnapshot()
    {
        // SO00001 is a known order from the seed data (seen in RuntimeSeedProviderIntegrationTests)
        var result = await _batchReader.ReadAsync("SO00001");

        Assert.True(result.IsSuccess);
        var snapshot = result.Value;

        Assert.Equal(new DateTimeOffset(2026, 8, 4, 12, 0, 0, TimeSpan.Zero), snapshot.ReadAtUtc);
        
        Assert.NotNull(snapshot.Order);
        Assert.Equal("SO00001", snapshot.Order.OrderReference);

        Assert.NotEmpty(snapshot.OrderItems);
        Assert.Contains(snapshot.OrderItems, item => item.ProductReference == "P002");

        Assert.NotEmpty(snapshot.Products);
        Assert.Contains(snapshot.Products, p => p.ProductReference == "P002");

        Assert.NotEmpty(snapshot.BomItems);
        // MAT-AHSAP-OTURAK should be included in BOM
        Assert.Contains(snapshot.BomItems, bom => bom.ComponentProductReference == "MAT-AHSAP-OTURAK");

        // The product references array passed to StockLevels should contain P002 and its BOM components.
        // We know P002 has 500 stock in the seed data.
        Assert.NotEmpty(snapshot.StockLevels);
        Assert.Contains(snapshot.StockLevels, stock => stock.ProductReference == "P002" && stock.OnHandQuantity == 500m);

        // Open POs and WorkOrders might be empty in seed data, but they should be non-null empty lists
        Assert.NotNull(snapshot.OpenPurchaseOrders);
        Assert.NotNull(snapshot.WorkOrders);
    }

    [Fact]
    public async Task ReadAsync_WithMissingOrder_ReturnsFailure()
    {
        var result = await _batchReader.ReadAsync("NON-EXISTENT-ORDER");

        Assert.True(result.IsFailure);
        Assert.Equal("ErpBatchReader.OrderNotFound", result.Error?.Code);
    }
}
