using App.Application.Contracts.Configuration;
using App.Application.Prediction;
using App.Application.Prediction.Resolvers;
using App.Domain.Abstractions;
using App.Domain.Prediction;

namespace App.Application.Tests.Prediction;

public class RuleBasedPredictionEngineTests
{
    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }

    private static RuleBasedPredictionEngine CreateEngine(DateTimeOffset now, long procurementFallbackMinutes = 960) =>
        new(
            new ProcurementResolver(),
            new CapacityResolver(),
            new FixedClock(now),
            new MvpAssumptionsOptions
            {
                Procurement = new ProcurementAssumptionsOptions { FallbackDurationMinutes = procurementFallbackMinutes }
            });

    private static PredictionContext CreateContext(
        decimal orderedQuantity = 10,
        IReadOnlyList<MaterialBomItem>? bomItems = null,
        IReadOnlyList<MaterialStock>? stockLevels = null,
        IReadOnlyList<MaterialPurchaseOrder>? openPurchaseOrders = null,
        IReadOnlyList<RoutingOperation>? routingOperations = null) => new(
        new OrderInput("ORD-1", "PROD-1", orderedQuantity, DateTimeOffset.UtcNow),
        new MaterialSnapshot(
            Array.Empty<MaterialProduct>(),
            bomItems ?? Array.Empty<MaterialBomItem>(),
            stockLevels ?? Array.Empty<MaterialStock>(),
            openPurchaseOrders ?? Array.Empty<MaterialPurchaseOrder>()),
        new RoutingSnapshot(routingOperations ?? Array.Empty<RoutingOperation>()),
        new CapacitySnapshot(),
        new CalendarSnapshot(),
        new ShippingSnapshot());

    [Fact]
    public void Run_WithInvalidOrder_StopsAfterValidateAndReturnsFailure()
    {
        var context = CreateContext(orderedQuantity: 0);
        var engine = CreateEngine(DateTimeOffset.UtcNow);

        var result = engine.Run(context);

        Assert.False(result.Success);
        Assert.Empty(result.MaterialShortages);
        Assert.Empty(result.AppliedFallbackReasons);
        Assert.Empty(context.Operations);
    }

    [Fact]
    public void Run_WithValidOrder_ExecutesAllSixStepsAndReturnsSuccess()
    {
        var bomItems = new[] { new MaterialBomItem("PROD-1", "COMP-1", 2) };
        var stockLevels = new[] { new MaterialStock("COMP-1", 5) };
        var routingOperations = new[] { new RoutingOperation("OP-1", 10, "WC-1", 30, Array.Empty<string>()) };
        var context = CreateContext(orderedQuantity: 10, bomItems: bomItems, stockLevels: stockLevels, routingOperations: routingOperations);
        var engine = CreateEngine(DateTimeOffset.UtcNow);

        var result = engine.Run(context);

        Assert.True(result.Success);
        Assert.Same(context, result.Context);

        var shortage = Assert.Single(result.MaterialShortages);
        Assert.Equal("COMP-1", shortage.ProductReference);
        Assert.Equal(15, shortage.ShortageQuantity);

        Assert.Single(context.Operations);
        Assert.Equal("OP-1", context.Operations[0].OperationReference);
        Assert.Equal(30, context.Operations[0].DurationMinutes);

        Assert.Contains(result.AppliedFallbackReasons, r => r.Contains("fallback lead time"));
        Assert.Contains(result.AppliedFallbackReasons, r => r.Contains("unlimited"));
    }

    [Fact]
    public void Run_WithOpenPoCoveringShortage_DoesNotApplyProcurementFallback()
    {
        var bomItems = new[] { new MaterialBomItem("PROD-1", "COMP-1", 2) };
        var stockLevels = new[] { new MaterialStock("COMP-1", 0) };
        var openPurchaseOrders = new[] { new MaterialPurchaseOrder("PO-1", "COMP-1", 100, DateTimeOffset.UtcNow.AddDays(1)) };
        var context = CreateContext(orderedQuantity: 10, bomItems: bomItems, stockLevels: stockLevels, openPurchaseOrders: openPurchaseOrders);
        var engine = CreateEngine(DateTimeOffset.UtcNow);

        var result = engine.Run(context);

        Assert.True(result.Success);
        Assert.Single(result.MaterialShortages);
        Assert.DoesNotContain(result.AppliedFallbackReasons, r => r.Contains("fallback lead time"));
        Assert.Contains(result.AppliedFallbackReasons, r => r.Contains("unlimited"));
    }

    [Fact]
    public void Run_CalledTwiceWithSameInputs_ProducesDeterministicResult()
    {
        var bomItems = new[] { new MaterialBomItem("PROD-1", "COMP-1", 2) };
        var stockLevels = new[] { new MaterialStock("COMP-1", 5) };
        var routingOperations = new[] { new RoutingOperation("OP-1", 10, "WC-1", 30, Array.Empty<string>()) };
        var now = DateTimeOffset.UtcNow;

        var firstContext = CreateContext(orderedQuantity: 10, bomItems: bomItems, stockLevels: stockLevels, routingOperations: routingOperations);
        var secondContext = CreateContext(orderedQuantity: 10, bomItems: bomItems, stockLevels: stockLevels, routingOperations: routingOperations);

        var firstResult = CreateEngine(now).Run(firstContext);
        var secondResult = CreateEngine(now).Run(secondContext);

        Assert.Equal(firstResult.Success, secondResult.Success);
        Assert.Equal(firstResult.MaterialShortages, secondResult.MaterialShortages);
        Assert.Equal(firstResult.AppliedFallbackReasons, secondResult.AppliedFallbackReasons);
        Assert.Equal(firstContext.Operations[0].OperationReference, secondContext.Operations[0].OperationReference);
        Assert.Equal(firstContext.Operations[0].DurationMinutes, secondContext.Operations[0].DurationMinutes);
    }

    [Fact]
    public void Run_WithNullContext_ThrowsArgumentNullException()
    {
        var engine = CreateEngine(DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentNullException>(() => engine.Run(null!));
    }
}
