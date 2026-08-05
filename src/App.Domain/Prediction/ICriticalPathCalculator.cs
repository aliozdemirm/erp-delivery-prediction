namespace App.Domain.Prediction;

public interface ICriticalPathCalculator
{
    CriticalPathOutcome Calculate(PredictionContext context);
}
