using App.Domain.Prediction;

namespace App.Application.Prediction.Steps;

/// <summary>
/// Engine-level precondition guard. Distinct from PredictionContextBuilder's ERP-sufficiency
/// checks (which gate whether a PredictionContext is built at all): this validates that the
/// context handed to the engine is safe for the engine itself to process, independent of
/// whether the caller went through the Builder.
/// </summary>
public sealed class ValidateOrderStep : IPredictionStep
{
    public bool IsValid { get; private set; } = true;

    public void Execute(PredictionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IsValid = context.OrderInput.Quantity > 0;
    }
}
