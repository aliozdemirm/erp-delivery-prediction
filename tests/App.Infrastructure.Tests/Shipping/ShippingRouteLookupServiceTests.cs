using App.Application.Abstractions.Erp;
using App.Application.Abstractions.Shipping;
using App.Application.Contracts.Erp;
using App.Infrastructure.Shipping;

namespace App.Infrastructure.Tests.Shipping;

public sealed class ShippingRouteLookupServiceTests
{
    [Fact]
    public async Task ExistingRouteReturnsFoundWithExactDuration()
    {
        var provider = new StubErpDataProvider(
            new ShippingDurationReadDto("WH-IST", "CUSTOMER-ANK", "STANDARD", 1_440L));
        var service = new ShippingRouteLookupService(provider);

        var result = await service.GetRouteAsync("WH-IST", "CUSTOMER-ANK", "STANDARD");

        var found = Assert.IsType<ShippingRouteLookupResult.Found>(result);
        Assert.Equal(1_440L, found.ShippingDurationMinutes);
        Assert.Equal(("WH-IST", "CUSTOMER-ANK", "STANDARD"), provider.LastLookup);
    }

    [Fact]
    public async Task MissingRouteReturnsNotFoundWithoutDuration()
    {
        var service = new ShippingRouteLookupService(new StubErpDataProvider(null));

        var result = await service.GetRouteAsync("WH-IST", "UNKNOWN", "STANDARD");

        Assert.IsType<ShippingRouteLookupResult.NotFound>(result);
        Assert.Empty(typeof(ShippingRouteLookupResult.NotFound).GetProperties());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task NonPositiveProviderDurationCannotProduceFound(long duration)
    {
        var provider = new StubErpDataProvider(
            new ShippingDurationReadDto("WH-IST", "CUSTOMER-ANK", "STANDARD", duration));
        var service = new ShippingRouteLookupService(provider);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetRouteAsync("WH-IST", "CUSTOMER-ANK", "STANDARD"));
    }

    [Fact]
    public void LookupResultHasExactlyFoundAndNotFoundOutcomes()
    {
        Assert.Equal(
            ["Found", "NotFound"],
            typeof(ShippingRouteLookupResult).GetNestedTypes()
                .Select(type => type.Name)
                .Order());
    }

    private sealed class StubErpDataProvider(ShippingDurationReadDto? shippingRoute)
        : IErpDataProvider
    {
        public (string Origin, string Destination, string Profile)? LastLookup { get; private set; }

        public Task<ShippingDurationReadDto?> GetShippingDurationAsync(
            string originReference,
            string destinationReference,
            string shippingProfileReference,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastLookup = (originReference, destinationReference, shippingProfileReference);
            return Task.FromResult(shippingRoute);
        }

        public Task<OrderReadDto?> GetOrderAsync(string orderReference, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<OrderSummaryReadDto>> GetOrderSummariesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<OrderItemReadDto>> GetOrderItemsAsync(string orderReference, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProductReadDto?> GetProductAsync(string productReference, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ProductReadDto>> GetProductsAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<BomItemReadDto>> GetProductBomAsync(string productReference, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<StockLevelReadDto>> GetStockLevelsAsync(IReadOnlyList<string> productReferences, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<OpenPurchaseOrderReadDto>> GetOpenPurchaseOrdersAsync(IReadOnlyList<string> productReferences, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<WorkOrderReadDto>> GetWorkOrdersAsync(string orderReference, IReadOnlyList<string> productReferences, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CapacityAndCalendarReadDto> GetCapacityAndCalendarAsync(IReadOnlyList<string> workCenterReferences, DateTimeOffset rangeStart, DateTimeOffset rangeEnd, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
