using App.Application.Contracts.Configuration;
using App.Application.Prediction;
using App.Application.Prediction.Resolvers;
using App.Application.Prediction.Steps;
using App.Domain.Prediction;

namespace App.Application.Tests.Prediction.Steps;

public class ResolveProcurementStepTests
{
    private static PredictionContext CreateContext(IReadOnlyList<MaterialPurchaseOrder> openPurchaseOrders) => new(
        new OrderInput("ORD-1", "PROD-1", 10, DateTimeOffset.UtcNow),
        new MaterialSnapshot(Array.Empty<MaterialProduct>(), Array.Empty<MaterialBomItem>(), Array.Empty<MaterialStock>(), openPurchaseOrders),
        new RoutingSnapshot(Array.Empty<RoutingOperation>()),
        new CapacitySnapshot(),
        new CalendarSnapshot(),
        new ShippingSnapshot());

    private static MvpAssumptionsOptions CreateOptions() => new()
    {
        Procurement = new ProcurementAssumptionsOptions { FallbackDurationMinutes = 960 }
    };

    [Fact]
    public void Execute_ShortageWithOpenPo_UsesResolverWithoutFallbackReason()
    {
        var expectedDate = DateTimeOffset.UtcNow.AddDays(1);
        var openPo = new MaterialPurchaseOrder("PO-1", "COMP-1", 50, expectedDate);
        var context = CreateContext(new[] { openPo });
        var shortages = new[] { new MaterialShortage("COMP-1", 20) };
        var step = new ResolveProcurementStep(shortages, new ProcurementResolver(), DateTimeOffset.UtcNow, CreateOptions());

        step.Execute(context);

        Assert.Empty(step.AppliedFallbackReasons);
    }

    [Fact]
    public void Execute_ShortageWithoutOpenPo_RecordsFallbackReason()
    {
        var context = CreateContext(Array.Empty<MaterialPurchaseOrder>());
        var shortages = new[] { new MaterialShortage("COMP-1", 20) };
        var step = new ResolveProcurementStep(shortages, new ProcurementResolver(), DateTimeOffset.UtcNow, CreateOptions());

        step.Execute(context);

        var reason = Assert.Single(step.AppliedFallbackReasons);
        Assert.Contains("fallback lead time", reason);
    }

    [Fact]
    public void Execute_WithNullContext_ThrowsArgumentNullException()
    {
        var step = new ResolveProcurementStep(Array.Empty<MaterialShortage>(), new ProcurementResolver(), DateTimeOffset.UtcNow, CreateOptions());

        Assert.Throws<ArgumentNullException>(() => step.Execute(null!));
    }
}
