using App.Api.Prediction.Demo;
using App.Application.Abstractions.Erp;
using App.Application.Abstractions.Shipping;
using App.Application.Contracts.Configuration;
using App.Application.Contracts.Erp;
using App.Application.Contracts.Prediction;
using App.Application.Prediction;
using App.Application.Prediction.Demo;
using App.Application.Prediction.Resolvers;
using App.Domain.Abstractions;
using App.Domain.Prediction;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace App.Api.Tests.Prediction.Demo;

public sealed class DemoWorkOrderWhatIfTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 7, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task FlagOff_MissingWorkOrders_ReturnsInsufficientWithoutDemoOperations()
    {
        var fixture = new WhatIfFixture(demoEnabled: false);

        var result = await fixture.Service.CalculateAsync(ValidRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal("Data.Insufficient", result.Error!.Code);
        Assert.DoesNotContain(
            fixture.ContextBuilder.ReceivedSnapshots.SelectMany(snapshot => snapshot.WorkOrders),
            workOrder => workOrder.WorkOrderReference.StartsWith("DEMO-", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FlagOn_MissingWorkOrders_ProducesSuccessfulDemoCriticalPathAndTimeline()
    {
        var fixture = new WhatIfFixture(demoEnabled: true);

        var result = await fixture.Service.CalculateAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("WHATIF-PROD-1", result.Value.OrderReference);
        Assert.Equal(["DEMO-OP-10", "DEMO-OP-20"], result.Value.CriticalPathOperations);
        Assert.Equal(["DEMO-OP-10", "DEMO-OP-20"], result.Value.Timeline.Select(item => item.OperationRef));

        var enrichedSnapshot = Assert.Single(fixture.ContextBuilder.ReceivedSnapshots);
        var demoWorkOrder = Assert.Single(enrichedSnapshot.WorkOrders);
        Assert.Equal("DEMO-WO-001", demoWorkOrder.WorkOrderReference);
        Assert.Equal("PROD-1", demoWorkOrder.ProductReference);
    }

    [Fact]
    public async Task CalculateAsync_PreservesCancellationTokenAcrossWhatIfReads()
    {
        var fixture = new WhatIfFixture(demoEnabled: true);
        using var cancellationSource = new CancellationTokenSource();

        await fixture.Service.CalculateAsync(ValidRequest(), cancellationSource.Token);

        fixture.ErpDataProvider.Verify(
            provider => provider.GetProductAsync("PROD-1", cancellationSource.Token),
            Times.Once);
        fixture.ErpDataProvider.Verify(
            provider => provider.GetProductBomAsync("PROD-1", cancellationSource.Token),
            Times.Once);
        fixture.ErpDataProvider.Verify(
            provider => provider.GetStockLevelsAsync(
                It.IsAny<IReadOnlyList<string>>(),
                cancellationSource.Token),
            Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RealWorkOrders_RemainAuthoritative_RegardlessOfFlag(bool demoEnabled)
    {
        var realSnapshot = CreateRealSnapshot();
        var recordingBuilder = new RecordingPredictionContextBuilder(new PredictionContextBuilder());
        IPredictionContextBuilder builder = demoEnabled
            ? new DemoWorkOrderPredictionContextBuilder(
                recordingBuilder,
                new DemoWorkOrderSnapshotEnricher(),
                NullLogger<DemoWorkOrderPredictionContextBuilder>.Instance)
            : recordingBuilder;

        var (status, context) = builder.Build(realSnapshot);

        Assert.Equal(DataSufficiency.Sufficient, status);
        Assert.NotNull(context);
        Assert.Equal(["REAL-OP-10"], context.RoutingSnapshot.Operations.Select(operation => operation.OperationReference));
        Assert.Same(realSnapshot, Assert.Single(recordingBuilder.ReceivedSnapshots));
        Assert.DoesNotContain(
            context.RoutingSnapshot.Operations,
            operation => operation.OperationReference.StartsWith("DEMO-", StringComparison.Ordinal));
    }

    private static WhatIfPredictionRequest ValidRequest() => new()
    {
        ProductReference = "PROD-1",
        Quantity = 2,
        LocationReference = "istanbul"
    };

    private static ErpBatchSnapshot CreateRealSnapshot()
    {
        var orderReference = "SO-REAL";
        return new ErpBatchSnapshot(
            Now,
            new OrderReadDto(orderReference, Now, null, null),
            [new OrderItemReadDto(orderReference, "L1", "PROD-1", 2, "EA")],
            [new ProductReadDto("PROD-1", null, "EA")],
            [new BomItemReadDto("PROD-1", "COMP-1", 1, "EA", "BOM-L1")],
            [new StockLevelReadDto("COMP-1", null, 10, 0, 10)],
            Array.Empty<OpenPurchaseOrderReadDto>(),
            [new WorkOrderReadDto(
                "REAL-WO-1",
                orderReference,
                "PROD-1",
                "Released",
                new RoutingReadDto(
                    "REAL-ROUTING-1",
                    [new OperationReadDto("REAL-OP-10", 10, "REAL-WC-1", 30, Array.Empty<string>())]))]);
    }

    private sealed class WhatIfFixture
    {
        public Mock<IErpDataProvider> ErpDataProvider { get; } = new();
        public RecordingPredictionContextBuilder ContextBuilder { get; }
        public WhatIfPredictionCalculationService Service { get; }

        public WhatIfFixture(bool demoEnabled)
        {
            ErpDataProvider
                .Setup(provider => provider.GetProductAsync("PROD-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProductReadDto("PROD-1", null, "EA"));
            ErpDataProvider
                .Setup(provider => provider.GetProductBomAsync("PROD-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync([new BomItemReadDto("PROD-1", "COMP-1", 1, "EA", "BOM-L1")]);
            ErpDataProvider
                .Setup(provider => provider.GetStockLevelsAsync(
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([new StockLevelReadDto("COMP-1", null, 10, 0, 10)]);

            ContextBuilder = new RecordingPredictionContextBuilder(new PredictionContextBuilder());
            IPredictionContextBuilder contextBuilder = demoEnabled
                ? new DemoWorkOrderPredictionContextBuilder(
                    ContextBuilder,
                    new DemoWorkOrderSnapshotEnricher(),
                    NullLogger<DemoWorkOrderPredictionContextBuilder>.Instance)
                : ContextBuilder;

            var clock = new Mock<IClock>();
            clock.Setup(value => value.UtcNow).Returns(Now);
            var options = new MvpAssumptionsOptions
            {
                WorkingCalendar = new WorkingCalendarAssumptionsOptions { MinutesPerDay = 480 },
                Procurement = new ProcurementAssumptionsOptions { FallbackDurationMinutes = 960 },
                Shipping = new ShippingAssumptionsOptions { FallbackDurationMinutes = 1440 }
            };
            var whatIfBuilder = new WhatIfPredictionContextBuilder(
                ErpDataProvider.Object,
                clock.Object,
                contextBuilder);
            var engine = new RuleBasedPredictionEngine(
                new ProcurementResolver(),
                new CapacityResolver(),
                clock.Object,
                options);
            var mapper = new PredictionResultMapper(clock.Object, options, new ShippingResolver());

            Service = new WhatIfPredictionCalculationService(
                whatIfBuilder,
                engine,
                new CriticalPathCalculator(),
                mapper,
                Mock.Of<IWhatIfShippingReferenceResolver>(),
                Mock.Of<IShippingRouteLookupService>());
        }
    }

    private sealed class RecordingPredictionContextBuilder(IPredictionContextBuilder inner)
        : IPredictionContextBuilder
    {
        public List<ErpBatchSnapshot> ReceivedSnapshots { get; } = [];

        public (DataSufficiency Status, PredictionContext? Context) Build(ErpBatchSnapshot snapshot)
        {
            ReceivedSnapshots.Add(snapshot);
            return inner.Build(snapshot);
        }
    }
}
