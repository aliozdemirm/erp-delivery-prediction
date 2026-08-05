using App.Application.Prediction.Resolvers;
using App.Domain.Prediction;

namespace App.Application.Prediction;

public abstract record ReadinessResult;

public sealed record Ready(
    PredictionContext Context,
    FallbackResult<DateTimeOffset> Procurement,
    FallbackResult<TimeSpan?> Shipping,
    FallbackResult<bool> Capacity) : ReadinessResult;

public sealed record InsufficientData(
    ReadinessFailureSource Source,
    string? Reason) : ReadinessResult;

public enum ReadinessFailureSource
{
    PredictionContext,
    Shipping
}
