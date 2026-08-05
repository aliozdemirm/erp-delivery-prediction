using App.Domain.Prediction;

namespace App.Application.Prediction;

public sealed record TimelineItem(
    string OperationRef,
    DateTimeOffset EstimatedStart,
    DateTimeOffset EstimatedEnd,
    bool IsCritical);

public sealed record RuleBasedPredictionResult(
    string OrderReference,
    DateTimeOffset EstimatedStart,
    DateTimeOffset EstimatedEnd,
    DateTimeOffset EstimatedDelivery,
    IReadOnlyList<string> CriticalPathOperations,
    IReadOnlyList<string> AppliedFallbackReasons,
    IReadOnlyList<MaterialShortage> Shortages,
    IReadOnlyList<TimelineItem> Timeline);
