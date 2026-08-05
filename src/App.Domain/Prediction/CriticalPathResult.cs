namespace App.Domain.Prediction;

public sealed record CriticalPathResult(
    IReadOnlyList<string> CriticalOperationRefs,
    long TotalWorkingMinutes,
    IReadOnlyList<OperationSchedule> Schedule);
