namespace App.Domain.Prediction;

public sealed class CriticalPathOutcome
{
    public CriticalPathStatus Status { get; }
    public CriticalPathResult? Result { get; }
    public string? FailureReason { get; }

    private CriticalPathOutcome(CriticalPathStatus status, CriticalPathResult? result, string? failureReason)
    {
        Status = status;
        Result = result;
        FailureReason = failureReason;
    }

    public static CriticalPathOutcome Success(CriticalPathResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new CriticalPathOutcome(CriticalPathStatus.Success, result, null);
    }

    public static CriticalPathOutcome Failure(CriticalPathStatus status, string failureReason)
    {
        if (status == CriticalPathStatus.Success)
        {
            throw new ArgumentException(
                "Failure outcomes cannot use CriticalPathStatus.Success.", nameof(status));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(failureReason);

        return new CriticalPathOutcome(status, null, failureReason);
    }
}
