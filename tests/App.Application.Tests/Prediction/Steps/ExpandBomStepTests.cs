using App.Application.Prediction.Steps;
using App.Domain.Prediction;

namespace App.Application.Tests.Prediction.Steps;

public class ExpandBomStepTests
{
    [Fact]
    public void Execute_FiltersBomItemsByOrderedProduct()
    {
        var bomItems = new[]
        {
            new MaterialBomItem("PROD-1", "COMP-1", 2),
            new MaterialBomItem("PROD-1", "COMP-2", 1),
            new MaterialBomItem("PROD-OTHER", "COMP-3", 5),
        };
        var context = new PredictionContext(
            new OrderInput("ORD-1", "PROD-1", 10, DateTimeOffset.UtcNow),
            new MaterialSnapshot(Array.Empty<MaterialProduct>(), bomItems, Array.Empty<MaterialStock>(), Array.Empty<MaterialPurchaseOrder>()),
            new RoutingSnapshot(Array.Empty<RoutingOperation>()),
            new CapacitySnapshot(),
            new CalendarSnapshot(),
            new ShippingSnapshot());
        var step = new ExpandBomStep();

        step.Execute(context);

        Assert.Equal(2, step.ExpandedBomItems.Count);
        Assert.All(step.ExpandedBomItems, item => Assert.Equal("PROD-1", item.ParentProductReference));
    }

    [Fact]
    public void Execute_WithNullContext_ThrowsArgumentNullException()
    {
        var step = new ExpandBomStep();

        Assert.Throws<ArgumentNullException>(() => step.Execute(null!));
    }
}
