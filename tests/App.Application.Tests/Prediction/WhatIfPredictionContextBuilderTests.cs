using App.Application.Abstractions.Erp;
using App.Application.Contracts.Erp;
using App.Application.Contracts.Prediction;
using App.Application.Prediction;
using App.Domain.Abstractions;
using App.Domain.Prediction;

namespace App.Application.Tests.Prediction;

public sealed class WhatIfPredictionContextBuilderTests
{
    [Fact]
    public async Task BuildAsync_WithValidRequestAndEmptyRouting_ReturnsInsufficientDataAndNullContext()
    {
        var now = new DateTimeOffset(
            2026,
            8,
            6,
            18,
            30,
            0,
            TimeSpan.Zero);

        var provider = new FakeErpDataProvider
        {
            ProductToReturn = new ProductReadDto(
                "PROD-1",
                null,
                "EA"),

            BomItemsToReturn =
            [
                new BomItemReadDto(
                    "PROD-1",
                    "COMP-1",
                    2m,
                    "EA",
                    null)
            ],

            StockLevelsToReturn =
            [
                new StockLevelReadDto(
                    "COMP-1",
                    "LOC-IST",
                    100m,
                    10m,
                    90m)
            ]
        };

        var contextBuilder = new CapturingPredictionContextBuilder();

        var builder = new WhatIfPredictionContextBuilder(
            provider,
            new FixedClock(now),
            contextBuilder);

        var request = new WhatIfPredictionRequest
        {
            ProductReference = "PROD-1",
            Quantity = 5m,
            LocationReference = "LOC-IST"
        };

        var (status, context) = await builder.BuildAsync(request);

        Assert.Equal(DataSufficiency.InsufficientData, status);
        Assert.Null(context);

        Assert.Equal(1, contextBuilder.InvocationCount);

        var snapshot = Assert.IsType<ErpBatchSnapshot>(
            contextBuilder.ReceivedSnapshot);

        Assert.Equal(now, snapshot.ReadAtUtc);
        Assert.Equal("WHATIF-PROD-1", snapshot.Order.OrderReference);
        Assert.Equal(now, snapshot.Order.RequestedDeliveryDateTime);

        var orderItem = Assert.Single(snapshot.OrderItems);

        Assert.Equal("WHATIF-PROD-1", orderItem.OrderReference);
        Assert.Equal("PROD-1", orderItem.ProductReference);
        Assert.Equal(5m, orderItem.OrderedQuantity);
        Assert.Equal("EA", orderItem.UnitOfMeasure);

        Assert.Single(snapshot.Products);
        Assert.Single(snapshot.BomItems);
        Assert.Single(snapshot.StockLevels);
        Assert.Empty(snapshot.OpenPurchaseOrders);
        Assert.Empty(snapshot.WorkOrders);

        Assert.Equal(0, provider.GetOrderInvocationCount);
        Assert.Equal(1, provider.GetProductInvocationCount);
        Assert.Equal(1, provider.GetBomInvocationCount);
        Assert.Equal(1, provider.GetStockInvocationCount);
    }

    [Fact]
    public async Task BuildAsync_WithDifferentLocationReference_DoesNotFilterReturnedStock()
    {
        var now = new DateTimeOffset(
            2026,
            8,
            6,
            18,
            30,
            0,
            TimeSpan.Zero);

        var provider = new FakeErpDataProvider
        {
            ProductToReturn = new ProductReadDto(
                "PROD-1",
                null,
                "EA"),

            BomItemsToReturn =
            [
                new BomItemReadDto(
                    "PROD-1",
                    "COMP-1",
                    2m,
                    "EA",
                    null)
            ],

            StockLevelsToReturn =
            [
                new StockLevelReadDto(
                    "COMP-1",
                    "LOC-IST",
                    100m,
                    10m,
                    90m)
            ]
        };

        var contextBuilder = new CapturingPredictionContextBuilder();

        var builder = new WhatIfPredictionContextBuilder(
            provider,
            new FixedClock(now),
            contextBuilder);

        var request = new WhatIfPredictionRequest
        {
            ProductReference = "PROD-1",
            Quantity = 5m,
            LocationReference = "LOC-ANK"
        };

        var (status, context) = await builder.BuildAsync(request);

        Assert.Equal(DataSufficiency.InsufficientData, status);
        Assert.Null(context);

        var snapshot = Assert.IsType<ErpBatchSnapshot>(
            contextBuilder.ReceivedSnapshot);

        var stockLevel = Assert.Single(snapshot.StockLevels);

        Assert.Equal("COMP-1", stockLevel.ProductReference);
        Assert.Equal("LOC-IST", stockLevel.LocationReference);
        Assert.Equal(90m, stockLevel.AvailableQuantity);

        Assert.Equal(1, provider.GetStockInvocationCount);
        Assert.Equal(1, contextBuilder.InvocationCount);
    }

    [Fact]
    public async Task BuildAsync_WhenProductIsNotFound_ReturnsInsufficientDataWithoutBuildingSnapshot()
    {
        var provider = new FakeErpDataProvider
        {
            ProductToReturn = null
        };

        var contextBuilder = new CapturingPredictionContextBuilder();

        var builder = new WhatIfPredictionContextBuilder(
            provider,
            new FixedClock(
                new DateTimeOffset(
                    2026,
                    8,
                    6,
                    18,
                    30,
                    0,
                    TimeSpan.Zero)),
            contextBuilder);

        var request = new WhatIfPredictionRequest
        {
            ProductReference = "MISSING-PROD",
            Quantity = 5m,
            LocationReference = "LOC-IST"
        };

        var (status, context) = await builder.BuildAsync(request);

        Assert.Equal(DataSufficiency.InsufficientData, status);
        Assert.Null(context);

        Assert.Equal(1, provider.GetProductInvocationCount);
        Assert.Equal(0, provider.GetBomInvocationCount);
        Assert.Equal(0, provider.GetStockInvocationCount);
        Assert.Equal(0, provider.GetOrderInvocationCount);

        Assert.Equal(0, contextBuilder.InvocationCount);
        Assert.Null(contextBuilder.ReceivedSnapshot);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BuildAsync_WithInvalidProductReference_ReturnsInsufficientDataWithoutReadingErp(
        string? productReference)
    {
        var provider = new FakeErpDataProvider
        {
            ProductToReturn = new ProductReadDto(
                "PROD-1",
                null,
                "EA")
        };

        var contextBuilder = new CapturingPredictionContextBuilder();

        var builder = new WhatIfPredictionContextBuilder(
            provider,
            new FixedClock(
                new DateTimeOffset(
                    2026,
                    8,
                    6,
                    18,
                    30,
                    0,
                    TimeSpan.Zero)),
            contextBuilder);

        var request = new WhatIfPredictionRequest
        {
            ProductReference = productReference!,
            Quantity = 5m,
            LocationReference = "LOC-IST"
        };

        var (status, context) = await builder.BuildAsync(request);

        Assert.Equal(DataSufficiency.InsufficientData, status);
        Assert.Null(context);

        Assert.Equal(0, provider.GetOrderInvocationCount);
        Assert.Equal(0, provider.GetProductInvocationCount);
        Assert.Equal(0, provider.GetBomInvocationCount);
        Assert.Equal(0, provider.GetStockInvocationCount);
        Assert.Equal(0, contextBuilder.InvocationCount);
        Assert.Null(contextBuilder.ReceivedSnapshot);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task BuildAsync_WithNonPositiveQuantity_ReturnsInsufficientDataWithoutReadingErp(
        decimal quantity)
    {
        var provider = new FakeErpDataProvider
        {
            ProductToReturn = new ProductReadDto(
                "PROD-1",
                null,
                "EA")
        };

        var contextBuilder = new CapturingPredictionContextBuilder();

        var builder = new WhatIfPredictionContextBuilder(
            provider,
            new FixedClock(
                new DateTimeOffset(
                    2026,
                    8,
                    6,
                    18,
                    30,
                    0,
                    TimeSpan.Zero)),
            contextBuilder);

        var request = new WhatIfPredictionRequest
        {
            ProductReference = "PROD-1",
            Quantity = quantity,
            LocationReference = "LOC-IST"
        };

        var (status, context) = await builder.BuildAsync(request);

        Assert.Equal(DataSufficiency.InsufficientData, status);
        Assert.Null(context);

        Assert.Equal(0, provider.GetOrderInvocationCount);
        Assert.Equal(0, provider.GetProductInvocationCount);
        Assert.Equal(0, provider.GetBomInvocationCount);
        Assert.Equal(0, provider.GetStockInvocationCount);
        Assert.Equal(0, contextBuilder.InvocationCount);
        Assert.Null(contextBuilder.ReceivedSnapshot);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BuildAsync_WithInvalidLocationReference_ReturnsInsufficientDataWithoutReadingErp(
        string? locationReference)
    {
        var provider = new FakeErpDataProvider
        {
            ProductToReturn = new ProductReadDto(
                "PROD-1",
                null,
                "EA")
        };

        var contextBuilder = new CapturingPredictionContextBuilder();

        var builder = new WhatIfPredictionContextBuilder(
            provider,
            new FixedClock(
                new DateTimeOffset(
                    2026,
                    8,
                    6,
                    18,
                    30,
                    0,
                    TimeSpan.Zero)),
            contextBuilder);

        var request = new WhatIfPredictionRequest
        {
            ProductReference = "PROD-1",
            Quantity = 5m,
            LocationReference = locationReference!
        };

        var (status, context) = await builder.BuildAsync(request);

        Assert.Equal(DataSufficiency.InsufficientData, status);
        Assert.Null(context);

        Assert.Equal(0, provider.GetOrderInvocationCount);
        Assert.Equal(0, provider.GetProductInvocationCount);
        Assert.Equal(0, provider.GetBomInvocationCount);
        Assert.Equal(0, provider.GetStockInvocationCount);
        Assert.Equal(0, contextBuilder.InvocationCount);
        Assert.Null(contextBuilder.ReceivedSnapshot);
    }

    [Fact]
    public async Task BuildAsync_WithSameRequest_CreatesDeterministicSyntheticOrderReference()
    {
        var now = new DateTimeOffset(
            2026,
            8,
            6,
            18,
            30,
            0,
            TimeSpan.Zero);

        var provider = new FakeErpDataProvider
        {
            ProductToReturn = new ProductReadDto(
                "PROD-1",
                null,
                "EA"),

            BomItemsToReturn =
            [
                new BomItemReadDto(
                    "PROD-1",
                    "COMP-1",
                    2m,
                    "EA",
                    null)
            ]
        };

        var firstContextBuilder = new CapturingPredictionContextBuilder();
        var secondContextBuilder = new CapturingPredictionContextBuilder();

        var firstBuilder = new WhatIfPredictionContextBuilder(
            provider,
            new FixedClock(now),
            firstContextBuilder);

        var secondBuilder = new WhatIfPredictionContextBuilder(
            provider,
            new FixedClock(now),
            secondContextBuilder);

        var request = new WhatIfPredictionRequest
        {
            ProductReference = "PROD-1",
            Quantity = 5m,
            LocationReference = "LOC-IST"
        };

        await firstBuilder.BuildAsync(request);
        await secondBuilder.BuildAsync(request);

        var firstSnapshot = Assert.IsType<ErpBatchSnapshot>(
            firstContextBuilder.ReceivedSnapshot);

        var secondSnapshot = Assert.IsType<ErpBatchSnapshot>(
            secondContextBuilder.ReceivedSnapshot);

        Assert.Equal(
            "WHATIF-PROD-1",
            firstSnapshot.Order.OrderReference);

        Assert.Equal(
            firstSnapshot.Order.OrderReference,
            secondSnapshot.Order.OrderReference);

        Assert.Equal(
            now,
            firstSnapshot.Order.RequestedDeliveryDateTime);

        Assert.Equal(
            now,
            secondSnapshot.Order.RequestedDeliveryDateTime);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class CapturingPredictionContextBuilder
        : IPredictionContextBuilder
    {
        private readonly PredictionContextBuilder _innerBuilder = new();

        public int InvocationCount { get; private set; }

        public ErpBatchSnapshot? ReceivedSnapshot { get; private set; }

        public (DataSufficiency Status, PredictionContext? Context) Build(
            ErpBatchSnapshot snapshot)
        {
            InvocationCount++;
            ReceivedSnapshot = snapshot;

            return _innerBuilder.Build(snapshot);
        }
    }

    private sealed class FakeErpDataProvider : IErpDataProvider
    {
        public ProductReadDto? ProductToReturn { get; init; }

        public IReadOnlyList<BomItemReadDto> BomItemsToReturn { get; init; } =
            Array.Empty<BomItemReadDto>();

        public IReadOnlyList<StockLevelReadDto> StockLevelsToReturn { get; init; } =
            Array.Empty<StockLevelReadDto>();

        public int GetOrderInvocationCount { get; private set; }

        public int GetProductInvocationCount { get; private set; }

        public int GetBomInvocationCount { get; private set; }

        public int GetStockInvocationCount { get; private set; }

        public Task<OrderReadDto?> GetOrderAsync(
            string orderReference,
            CancellationToken cancellationToken)
        {
            GetOrderInvocationCount++;

            throw new InvalidOperationException(
                "What-If context construction must not perform an ERP order lookup.");
        }

        public Task<IReadOnlyList<OrderSummaryReadDto>> GetOrderSummariesAsync(
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<OrderItemReadDto>> GetOrderItemsAsync(
            string orderReference,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException(
                "What-If context construction must not read ERP order items.");

        public Task<ProductReadDto?> GetProductAsync(
            string productReference,
            CancellationToken cancellationToken)
        {
            GetProductInvocationCount++;

            return Task.FromResult(ProductToReturn);
        }

        public Task<IReadOnlyList<BomItemReadDto>> GetProductBomAsync(
            string productReference,
            CancellationToken cancellationToken)
        {
            GetBomInvocationCount++;

            return Task.FromResult(BomItemsToReturn);
        }

        public Task<IReadOnlyList<StockLevelReadDto>> GetStockLevelsAsync(
            IReadOnlyList<string> productReferences,
            CancellationToken cancellationToken)
        {
            GetStockInvocationCount++;

            return Task.FromResult(StockLevelsToReturn);
        }

        public Task<IReadOnlyList<OpenPurchaseOrderReadDto>>
            GetOpenPurchaseOrdersAsync(
                IReadOnlyList<string> productReferences,
                CancellationToken cancellationToken) =>
            throw new InvalidOperationException(
                "Open purchase-order enrichment is not part of WIF-02.");

        public Task<IReadOnlyList<WorkOrderReadDto>> GetWorkOrdersAsync(
            string orderReference,
            IReadOnlyList<string> productReferences,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException(
                "Work-order and routing enrichment is not part of WIF-02.");

        public Task<CapacityAndCalendarReadDto> GetCapacityAndCalendarAsync(
            IReadOnlyList<string> workCenterReferences,
            DateTimeOffset rangeStart,
            DateTimeOffset rangeEnd,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ShippingDurationReadDto?> GetShippingDurationAsync(
            string originReference,
            string destinationReference,
            string shippingProfileReference,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }
}