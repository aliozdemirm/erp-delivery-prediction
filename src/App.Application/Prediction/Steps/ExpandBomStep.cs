using App.Domain.Prediction;

namespace App.Application.Prediction.Steps;

/// <summary>
/// Filters the full BOM snapshot down to the lines belonging to the ordered product.
/// Output is exposed via <see cref="ExpandedBomItems"/> for CheckStockStep to consume.
/// </summary>
public sealed class ExpandBomStep : IPredictionStep
{
    public IReadOnlyList<MaterialBomItem> ExpandedBomItems { get; private set; } = Array.Empty<MaterialBomItem>();

    public void Execute(PredictionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ExpandedBomItems = context.MaterialSnapshot.BomItems
            .Where(bomItem => bomItem.ParentProductReference == context.OrderInput.ProductReference)
            .ToList();
    }
}
