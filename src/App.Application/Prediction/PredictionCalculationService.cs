using App.Application.Abstractions.Erp;
using App.Application.Common;
using App.Domain.Prediction;

namespace App.Application.Prediction;

public sealed class PredictionCalculationService : IPredictionCalculationService
{
    private readonly IErpBatchReader _erpBatchReader;
    private readonly PredictionContextBuilder _contextBuilder;
    private readonly RuleBasedPredictionEngine _predictionEngine;
    private readonly ICriticalPathCalculator _criticalPathCalculator;
    private readonly PredictionResultMapper _resultMapper;

    public PredictionCalculationService(
        IErpBatchReader erpBatchReader,
        PredictionContextBuilder contextBuilder,
        RuleBasedPredictionEngine predictionEngine,
        ICriticalPathCalculator criticalPathCalculator,
        PredictionResultMapper resultMapper)
    {
        _erpBatchReader = erpBatchReader ?? throw new ArgumentNullException(nameof(erpBatchReader));
        _contextBuilder = contextBuilder ?? throw new ArgumentNullException(nameof(contextBuilder));
        _predictionEngine = predictionEngine ?? throw new ArgumentNullException(nameof(predictionEngine));
        _criticalPathCalculator = criticalPathCalculator ?? throw new ArgumentNullException(nameof(criticalPathCalculator));
        _resultMapper = resultMapper ?? throw new ArgumentNullException(nameof(resultMapper));
    }

    public async Task<Result<RuleBasedPredictionResult>> CalculateAsync(string orderReference, CancellationToken cancellationToken = default)
    {
        // 1. Read ERP Snapshot
        var snapshotResult = await _erpBatchReader.ReadAsync(orderReference, cancellationToken);
        if (!snapshotResult.IsSuccess)
        {
            return Result<RuleBasedPredictionResult>.Failure(snapshotResult.Error!);
        }

        // 2. Build Context
        var (status, context) = _contextBuilder.Build(snapshotResult.Value!);
        if (status != DataSufficiency.Sufficient || context is null)
        {
            return Result<RuleBasedPredictionResult>.Failure(new Error("Data.Insufficient", "ERP data is insufficient to run prediction.", ErrorType.Validation));
        }

        // 3. Rule Engine
        var engineResult = _predictionEngine.Run(context);
        if (!engineResult.Success)
        {
            return Result<RuleBasedPredictionResult>.Failure(new Error("RuleEngine.Failed", "Order failed rule validation.", ErrorType.Validation));
        }

        // 4. Critical Path Method
        var cpmOutcome = _criticalPathCalculator.Calculate(engineResult.Context);

        // 5-7. Calendar, shipping and result mapping
        return _resultMapper.Map(orderReference, engineResult, cpmOutcome);
    }
}
