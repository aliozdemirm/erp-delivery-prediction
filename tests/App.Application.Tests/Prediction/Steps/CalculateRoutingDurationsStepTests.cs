using App.Application.Prediction.Steps;
using App.Domain.Prediction;

namespace App.Application.Tests.Prediction.Steps;

public class CalculateRoutingDurationsStepTests
{
    [Fact]
    public void Execute_AddsOneOperationPerRoutingOperation_WithMatchingDuration()
    {
        var operations = new[]
        {
            new RoutingOperation("OP-1", 10, "WC-1", 30, Array.Empty<string>()),
            new RoutingOperation("OP-2", 20, "WC-1", 45, new[] { "OP-1" }),
        };
        var context = new PredictionContext(
            new OrderInput("ORD-1", "PROD-1", 10, DateTimeOffset.UtcNow),
            new MaterialSnapshot(Array.Empty<MaterialProduct>(), Array.Empty<MaterialBomItem>(), Array.Empty<MaterialStock>(), Array.Empty<MaterialPurchaseOrder>()),
            new RoutingSnapshot(operations),
            new CapacitySnapshot(),
            new CalendarSnapshot(),
            new ShippingSnapshot());
        var step = new CalculateRoutingDurationsStep();

        step.Execute(context);

        Assert.Equal(2, context.Operations.Count);
        Assert.Equal("OP-1", context.Operations[0].OperationReference);
        Assert.Equal(30, context.Operations[0].DurationMinutes);
        Assert.Equal("OP-2", context.Operations[1].OperationReference);
        Assert.Equal(45, context.Operations[1].DurationMinutes);
    }

    [Fact]
    public void Execute_WithNullContext_ThrowsArgumentNullException()
    {
        var step = new CalculateRoutingDurationsStep();

        Assert.Throws<ArgumentNullException>(() => step.Execute(null!));
    }
}
