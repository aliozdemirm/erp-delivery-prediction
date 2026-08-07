using App.Application.Abstractions.Erp;
using App.Application.Abstractions.Shipping;
using App.Application.Common;
using App.Application.Contracts.Configuration;
using App.Application.Contracts.Erp;
using App.Application.Contracts.Prediction;
using App.Application.Prediction;
using App.Application.Prediction.Resolvers;
using App.Domain.Abstractions;
using App.Domain.Prediction;
using Moq;

namespace App.Application.Tests.Prediction;

public sealed class WhatIfPredictionCalculationServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CalculateAsync_WhenRequestIsInvalid_ReturnsInsufficientWithoutCallingCpm()
    {
        var fixture = new Fixture();

        var result = await fixture.Service.CalculateAsync(new WhatIfPredictionRequest
        {
            ProductReference = string.Empty,
            Quantity = 1,
            LocationReference = "LOC-1"
        });

        AssertInsufficient(result);
        fixture.CriticalPathCalculator.Verify(
            calculator => calculator.Calculate(It.IsAny<PredictionContext>()),
            Times.Never);
        fixture.ErpDataProvider.Verify(
            provider => provider.GetProductAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CalculateAsync_WhenContextDataIsInsufficient_ReturnsFailureWithoutCallingCpm()
    {
        var fixture = new Fixture(DataSufficiency.InsufficientData, null);

        var result = await fixture.Service.CalculateAsync(ValidRequest());

        AssertInsufficient(result);
        fixture.CriticalPathCalculator.Verify(
            calculator => calculator.Calculate(It.IsAny<PredictionContext>()),
            Times.Never);
    }

    [Fact]
    public async Task CalculateAsync_WhenContextIsNullDespiteSufficientStatus_ReturnsFailureWithoutCallingCpm()
    {
        var fixture = new Fixture(DataSufficiency.Sufficient, null);

        var result = await fixture.Service.CalculateAsync(ValidRequest());

        AssertInsufficient(result);
        fixture.CriticalPathCalculator.Verify(
            calculator => calculator.Calculate(It.IsAny<PredictionContext>()),
            Times.Never);
    }

    [Fact]
    public async Task CalculateAsync_WhenProductIsMissing_ReturnsInsufficientWithoutCallingCpm()
    {
        var fixture = new Fixture(productMissing: true);

        var result = await fixture.Service.CalculateAsync(ValidRequest());

        AssertInsufficient(result);
        fixture.CriticalPathCalculator.Verify(
            calculator => calculator.Calculate(It.IsAny<PredictionContext>()),
            Times.Never);
        fixture.ContextBuilder.Verify(
            builder => builder.Build(It.IsAny<ErpBatchSnapshot>()),
            Times.Never);
    }

    [Fact]
    public async Task CalculateAsync_WhenRuleEngineFails_PreservesErrorConventionAndDoesNotCallCpm()
    {
        var fixture = new Fixture(DataSufficiency.Sufficient, CreateContext(quantity: 0));

        var result = await fixture.Service.CalculateAsync(ValidRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal("RuleEngine.Failed", result.Error!.Code);
        fixture.CriticalPathCalculator.Verify(
            calculator => calculator.Calculate(It.IsAny<PredictionContext>()),
            Times.Never);
    }

    [Fact]
    public async Task CalculateAsync_WhenPipelineSucceeds_MapsExistingPredictionResult()
    {
        var context = CreateContext();
        var fixture = new Fixture(DataSufficiency.Sufficient, context);
        fixture.CriticalPathCalculator
            .Setup(calculator => calculator.Calculate(context))
            .Returns(CriticalPathOutcome.Success(
                new CriticalPathResult(
                    ["OP-1"],
                    60,
                    [new OperationSchedule("OP-1", 0, 60, 0, 60, 0)])));

        var result = await fixture.Service.CalculateAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("WHATIF-PROD-1", result.Value.OrderReference);
        Assert.Equal(Now, result.Value.EstimatedStart);
        Assert.Equal(Now.AddMinutes(60), result.Value.EstimatedEnd);
        Assert.Single(result.Value.Timeline);
        Assert.Equal("OP-1", result.Value.Timeline[0].OperationRef);
        Assert.True(result.Value.Timeline[0].IsCritical);
        fixture.CriticalPathCalculator.Verify(
            calculator => calculator.Calculate(context),
            Times.Once);
    }

    [Fact]
    public async Task CalculateAsync_WhenRealRouteIsFound_UsesRealDuration()
    {
        var context = CreateContext();
        var fixture = new Fixture(DataSufficiency.Sufficient, context);
        fixture.SetupSuccessfulCpm(context);
        fixture.ShippingReferenceResolver.Setup(resolver => resolver.Resolve("istanbul"))
            .Returns(new WhatIfShippingRouteReferences("ORIGIN", "DESTINATION", "PROFILE"));
        fixture.ShippingRouteLookup.Setup(service => service.GetRouteAsync("ORIGIN", "DESTINATION", "PROFILE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShippingRouteLookupResult.Found(120));

        var result = await fixture.Service.CalculateAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal(result.Value.EstimatedEnd.AddMinutes(120), result.Value.EstimatedDelivery);
        Assert.DoesNotContain(result.Value.AppliedFallbackReasons, reason => reason.Contains("Shipping", StringComparison.OrdinalIgnoreCase));
        fixture.ShippingRouteLookup.Verify(service => service.GetRouteAsync("ORIGIN", "DESTINATION", "PROFILE", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CalculateAsync_WhenMappingIsUnavailable_PreservesFallbackWithoutLookup()
    {
        var context = CreateContext();
        var fixture = new Fixture(DataSufficiency.Sufficient, context);
        fixture.SetupSuccessfulCpm(context);

        var result = await fixture.Service.CalculateAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal(result.Value.EstimatedEnd.AddMinutes(1440), result.Value.EstimatedDelivery);
        fixture.ShippingRouteLookup.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(CriticalPathStatus.CycleDetected, "CPM.CycleDetected")]
    [InlineData(CriticalPathStatus.MissingPredecessorReference, "CPM.MissingPredecessorReference")]
    public async Task CalculateAsync_WhenCriticalPathFails_ReturnsExistingSpecificError(
        CriticalPathStatus status,
        string expectedCode)
    {
        var context = CreateContext();
        var fixture = new Fixture(DataSufficiency.Sufficient, context);
        fixture.CriticalPathCalculator
            .Setup(calculator => calculator.Calculate(context))
            .Returns(CriticalPathOutcome.Failure(status, "Graph failure."));

        var result = await fixture.Service.CalculateAsync(ValidRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCode, result.Error!.Code);
        Assert.Contains("Graph failure.", result.Error.Message);
    }

    private static void AssertInsufficient(Result<RuleBasedPredictionResult> result)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal("Data.Insufficient", result.Error!.Code);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    private static WhatIfPredictionRequest ValidRequest() => new()
    {
        ProductReference = "PROD-1",
        Quantity = 1,
        LocationReference = "istanbul"
    };

    private static PredictionContext CreateContext(decimal quantity = 1) => new(
        new OrderInput("WHATIF-PROD-1", "PROD-1", quantity, Now),
        new MaterialSnapshot(
            [new MaterialProduct("PROD-1", "EA")],
            [new MaterialBomItem("PROD-1", "COMP-1", 1)],
            [new MaterialStock("COMP-1", 10)],
            Array.Empty<MaterialPurchaseOrder>()),
        new RoutingSnapshot(
            [new RoutingOperation("OP-1", 1, "WC-1", 60, Array.Empty<string>())]),
        new CapacitySnapshot(),
        new CalendarSnapshot(),
        new ShippingSnapshot());

    private sealed class Fixture
    {
        public Mock<IErpDataProvider> ErpDataProvider { get; } = new();
        public Mock<IPredictionContextBuilder> ContextBuilder { get; } = new();
        public Mock<ICriticalPathCalculator> CriticalPathCalculator { get; } = new();
        public Mock<IWhatIfShippingReferenceResolver> ShippingReferenceResolver { get; } = new();
        public Mock<IShippingRouteLookupService> ShippingRouteLookup { get; } = new();
        public WhatIfPredictionCalculationService Service { get; }

        public Fixture(
            DataSufficiency status = DataSufficiency.InsufficientData,
            PredictionContext? context = null,
            bool productMissing = false)
        {
            ProductReadDto? product = productMissing
                ? null
                : new ProductReadDto("PROD-1", null, "EA");
            ErpDataProvider
                .Setup(provider => provider.GetProductAsync("PROD-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
            ErpDataProvider
                .Setup(provider => provider.GetProductBomAsync("PROD-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<BomItemReadDto>());
            ErpDataProvider
                .Setup(provider => provider.GetStockLevelsAsync(
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<StockLevelReadDto>());
            ContextBuilder
                .Setup(builder => builder.Build(It.IsAny<ErpBatchSnapshot>()))
                .Returns((status, context));

            var clock = new Mock<IClock>();
            clock.Setup(value => value.UtcNow).Returns(Now);
            var options = new MvpAssumptionsOptions
            {
                WorkingCalendar = new WorkingCalendarAssumptionsOptions { MinutesPerDay = 480 },
                Procurement = new ProcurementAssumptionsOptions { FallbackDurationMinutes = 960 },
                Shipping = new ShippingAssumptionsOptions { FallbackDurationMinutes = 1440 }
            };
            var engine = new RuleBasedPredictionEngine(
                new ProcurementResolver(),
                new CapacityResolver(),
                clock.Object,
                options);
            var mapper = new PredictionResultMapper(clock.Object, options, new ShippingResolver());
            var builder = new WhatIfPredictionContextBuilder(
                ErpDataProvider.Object,
                clock.Object,
                ContextBuilder.Object);

            Service = new WhatIfPredictionCalculationService(
                builder,
                engine,
                CriticalPathCalculator.Object,
                mapper,
                ShippingReferenceResolver.Object,
                ShippingRouteLookup.Object);
        }

        public void SetupSuccessfulCpm(PredictionContext context) => CriticalPathCalculator
            .Setup(calculator => calculator.Calculate(context))
            .Returns(CriticalPathOutcome.Success(new CriticalPathResult(
                ["OP-1"], 60, [new OperationSchedule("OP-1", 0, 60, 0, 60, 0)])));
    }
}
