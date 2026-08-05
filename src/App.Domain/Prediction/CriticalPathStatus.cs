namespace App.Domain.Prediction;

public enum CriticalPathStatus
{
    Success,
    CycleDetected,
    MissingPredecessorReference
}
