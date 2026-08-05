using App.Application.Common;

namespace App.Application.Prediction;

public interface IPredictionCalculationService
{
    Task<Result<RuleBasedPredictionResult>> CalculateAsync(string orderReference, CancellationToken cancellationToken = default);
}
