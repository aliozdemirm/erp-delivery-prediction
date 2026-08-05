using App.Application.Prediction.Resolvers;
using App.Application.Prediction.Steps;
using App.Domain.Prediction;

namespace App.Application.Tests.Prediction.Steps;

public class ApplyCapacityStepTests
{
    private static PredictionContext CreateContext() => new(
        new OrderInput("ORD-1", "PROD-1", 10, DateTimeOffset.UtcNow),
        new MaterialSnapshot(Array.Empty<MaterialProduct>(), Array.Empty<MaterialBomItem>(), Array.Empty<MaterialStock>(), Array.Empty<MaterialPurchaseOrder>()),
        new RoutingSnapshot(Array.Empty<RoutingOperation>()),
        new CapacitySnapshot(),
        new CalendarSnapshot(),
        new ShippingSnapshot());

    [Fact]
    public void Execute_RecordsFallbackReasonFromResolver()
    {
        var step = new ApplyCapacityStep(new CapacityResolver());

        step.Execute(CreateContext());

        Assert.NotNull(step.AppliedFallbackReason);
        Assert.Contains("unlimited", step.AppliedFallbackReason);
    }

    [Fact]
    public void Execute_WithNullContext_ThrowsArgumentNullException()
    {
        var step = new ApplyCapacityStep(new CapacityResolver());

        Assert.Throws<ArgumentNullException>(() => step.Execute(null!));
    }

    [Fact]
    public void Constructor_WithNullResolver_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ApplyCapacityStep(null!));
    }
}
