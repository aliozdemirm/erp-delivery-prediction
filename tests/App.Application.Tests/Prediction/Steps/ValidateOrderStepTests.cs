using App.Application.Prediction.Steps;
using App.Domain.Prediction;

namespace App.Application.Tests.Prediction.Steps;

public class ValidateOrderStepTests
{
    private static PredictionContext CreateContext(decimal quantity) => new(
        new OrderInput("ORD-1", "PROD-1", quantity, DateTimeOffset.UtcNow),
        new MaterialSnapshot(Array.Empty<MaterialProduct>(), Array.Empty<MaterialBomItem>(), Array.Empty<MaterialStock>(), Array.Empty<MaterialPurchaseOrder>()),
        new RoutingSnapshot(Array.Empty<RoutingOperation>()),
        new CapacitySnapshot(),
        new CalendarSnapshot(),
        new ShippingSnapshot());

    [Fact]
    public void Execute_WithPositiveQuantity_SetsIsValidTrue()
    {
        var step = new ValidateOrderStep();

        step.Execute(CreateContext(10));

        Assert.True(step.IsValid);
    }

    [Fact]
    public void Execute_WithZeroQuantity_SetsIsValidFalse()
    {
        var step = new ValidateOrderStep();

        step.Execute(CreateContext(0));

        Assert.False(step.IsValid);
    }

    [Fact]
    public void Execute_WithNullContext_ThrowsArgumentNullException()
    {
        var step = new ValidateOrderStep();

        Assert.Throws<ArgumentNullException>(() => step.Execute(null!));
    }
}
