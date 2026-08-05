using App.Domain.Prediction;

namespace App.Application.Prediction.Steps;

/// <summary>
/// Compares required component quantities (BOM line x order quantity) against on-hand stock
/// and produces the shortage list. Output is exposed via <see cref="Shortages"/> for
/// ResolveProcurementStep to consume.
/// </summary>
public sealed class CheckStockStep : IPredictionStep
{
    private readonly IReadOnlyList<MaterialBomItem> _expandedBomItems;

    public IReadOnlyList<MaterialShortage> Shortages { get; private set; } = Array.Empty<MaterialShortage>();

    public CheckStockStep(IReadOnlyList<MaterialBomItem> expandedBomItems)
    {
        ArgumentNullException.ThrowIfNull(expandedBomItems);
        _expandedBomItems = expandedBomItems;
    }

    public void Execute(PredictionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var orderedQuantity = context.OrderInput.Quantity;
        var shortages = new List<MaterialShortage>();

        foreach (var bomItem in _expandedBomItems)
        {
            var requiredQuantity = bomItem.RequiredQuantity * orderedQuantity;
            var stock = context.MaterialSnapshot.StockLevels
                .FirstOrDefault(s => s.ProductReference == bomItem.ComponentProductReference);
            var availableQuantity = stock?.AvailableQuantity ?? 0m;

            if (availableQuantity < requiredQuantity)
            {
                shortages.Add(new MaterialShortage(bomItem.ComponentProductReference, requiredQuantity - availableQuantity));
            }
        }

        Shortages = shortages;
    }
}
