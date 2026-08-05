using App.Domain.Prediction;

namespace App.Application.Prediction.Steps;

/// <summary>
/// Maps each routing operation's standard duration into the PredictionContext's Operations
/// list. No capacity/calendar adjustment and no CPM/dependency graph — those are out of
/// scope for T-369.
/// </summary>
public sealed class CalculateRoutingDurationsStep : IPredictionStep
{
    public void Execute(PredictionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var routingOperation in context.RoutingSnapshot.Operations)
        {
            context.AddOperation(new Operation(routingOperation.OperationReference, routingOperation.StandardDurationMinutes));
        }
    }
}
