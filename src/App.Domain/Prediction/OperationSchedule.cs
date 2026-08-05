namespace App.Domain.Prediction;

public sealed record OperationSchedule(
    string OperationRef,
    long EarliestStartMinutes,
    long EarliestFinishMinutes,
    long LatestStartMinutes,
    long LatestFinishMinutes,
    long SlackMinutes);
