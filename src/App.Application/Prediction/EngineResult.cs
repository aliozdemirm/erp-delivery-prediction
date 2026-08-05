using App.Domain.Prediction;

namespace App.Application.Prediction;

public sealed record EngineResult(
    bool Success,
    PredictionContext Context,
    IReadOnlyList<MaterialShortage> MaterialShortages,
    IReadOnlyList<string> AppliedFallbackReasons);
