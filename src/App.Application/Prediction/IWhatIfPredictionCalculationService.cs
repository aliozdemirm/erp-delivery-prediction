using App.Application.Common;
using App.Application.Contracts.Prediction;

namespace App.Application.Prediction;

public interface IWhatIfPredictionCalculationService
{
    Task<Result<RuleBasedPredictionResult>> CalculateAsync(
        WhatIfPredictionRequest request,
        CancellationToken cancellationToken = default);
}
