namespace App.Domain.Prediction;

/// <summary>
/// Element type for PredictionContext.Operations (SAD §9.4). Extended by T-369 with the
/// routing operation reference and its calculated duration; the parameterless constructor
/// is kept so existing callers are unaffected.
/// </summary>
public sealed class Operation
{
    public string? OperationReference { get; }

    public long DurationMinutes { get; }

    public Operation()
    {
    }

    public Operation(string operationReference, long durationMinutes)
    {
        ArgumentNullException.ThrowIfNull(operationReference);
        if (durationMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), durationMinutes, "Duration cannot be negative.");
        }

        OperationReference = operationReference;
        DurationMinutes = durationMinutes;
    }
}
