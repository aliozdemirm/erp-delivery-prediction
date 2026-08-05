namespace App.Application.Contracts.Prediction;

public sealed record WhatIfPredictionRequest : PredictionRequest
{
    public required string ProductReference { get; init; }

    public required decimal Quantity { get; init; }

    public required string LocationReference { get; init; }
}
