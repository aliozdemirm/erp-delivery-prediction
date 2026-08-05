namespace App.Application.Contracts.Prediction;

public sealed record OrderReferencePredictionRequest : PredictionRequest
{
    public required string OrderReference { get; init; }
}
