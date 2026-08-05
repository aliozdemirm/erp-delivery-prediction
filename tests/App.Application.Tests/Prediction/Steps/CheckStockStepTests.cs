using App.Application.Prediction.Steps;
using App.Domain.Prediction;

namespace App.Application.Tests.Prediction.Steps;

public class CheckStockStepTests
{
    private static PredictionContext CreateContext(decimal orderedQuantity, IReadOnlyList<MaterialStock> stockLevels) => new(
        new OrderInput("ORD-1", "PROD-1", orderedQuantity, DateTimeOffset.UtcNow),
        new MaterialSnapshot(Array.Empty<MaterialProduct>(), Array.Empty<MaterialBomItem>(), stockLevels, Array.Empty<MaterialPurchaseOrder>()),
        new RoutingSnapshot(Array.Empty<RoutingOperation>()),
        new CapacitySnapshot(),
        new CalendarSnapshot(),
        new ShippingSnapshot());

    [Fact]
    public void Execute_WithInsufficientStock_ProducesShortage()
    {
        var bomItems = new[] { new MaterialBomItem("PROD-1", "COMP-1", 2) };
        var stockLevels = new[] { new MaterialStock("COMP-1", 5) };
        var context = CreateContext(10, stockLevels);
        var step = new CheckStockStep(bomItems);

        step.Execute(context);

        var shortage = Assert.Single(step.Shortages);
        Assert.Equal("COMP-1", shortage.ProductReference);
        Assert.Equal(15, shortage.ShortageQuantity);
    }

    [Fact]
    public void Execute_WithSufficientStock_ProducesNoShortage()
    {
        var bomItems = new[] { new MaterialBomItem("PROD-1", "COMP-1", 2) };
        var stockLevels = new[] { new MaterialStock("COMP-1", 100) };
        var context = CreateContext(10, stockLevels);
        var step = new CheckStockStep(bomItems);

        step.Execute(context);

        Assert.Empty(step.Shortages);
    }

    [Fact]
    public void Execute_WithNoMatchingStockRecord_TreatsAvailableAsZero()
    {
        var bomItems = new[] { new MaterialBomItem("PROD-1", "COMP-1", 1) };
        var context = CreateContext(3, Array.Empty<MaterialStock>());
        var step = new CheckStockStep(bomItems);

        step.Execute(context);

        var shortage = Assert.Single(step.Shortages);
        Assert.Equal(3, shortage.ShortageQuantity);
    }

    [Fact]
    public void Execute_WithNullContext_ThrowsArgumentNullException()
    {
        var step = new CheckStockStep(Array.Empty<MaterialBomItem>());

        Assert.Throws<ArgumentNullException>(() => step.Execute(null!));
    }

    [Fact]
    public void Constructor_WithNullExpandedBomItems_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CheckStockStep(null!));
    }
}
