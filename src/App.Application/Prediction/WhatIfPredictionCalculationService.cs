using App.Application.Common;
using App.Application.Abstractions.Shipping;
using App.Application.Contracts.Prediction;
using App.Domain.Prediction;

namespace App.Application.Prediction;

public sealed class WhatIfPredictionCalculationService : IWhatIfPredictionCalculationService
{
    private static readonly Error InsufficientDataError = new(
        "Data.Insufficient",
        "ERP data is insufficient to run prediction.",
        ErrorType.Validation);

    private readonly WhatIfPredictionContextBuilder _contextBuilder;
    private readonly RuleBasedPredictionEngine _predictionEngine;
    private readonly ICriticalPathCalculator _criticalPathCalculator;
    private readonly PredictionResultMapper _resultMapper;
    private readonly IWhatIfShippingReferenceResolver _shippingReferenceResolver;
    private readonly IShippingRouteLookupService _shippingRouteLookupService;

    public WhatIfPredictionCalculationService(
        WhatIfPredictionContextBuilder contextBuilder,
        RuleBasedPredictionEngine predictionEngine,
        ICriticalPathCalculator criticalPathCalculator,
        PredictionResultMapper resultMapper,
        IWhatIfShippingReferenceResolver shippingReferenceResolver,
        IShippingRouteLookupService shippingRouteLookupService)
    {
        _contextBuilder = contextBuilder ?? throw new ArgumentNullException(nameof(contextBuilder));
        _predictionEngine = predictionEngine ?? throw new ArgumentNullException(nameof(predictionEngine));
        _criticalPathCalculator = criticalPathCalculator ?? throw new ArgumentNullException(nameof(criticalPathCalculator));
        _resultMapper = resultMapper ?? throw new ArgumentNullException(nameof(resultMapper));
        _shippingReferenceResolver = shippingReferenceResolver ?? throw new ArgumentNullException(nameof(shippingReferenceResolver));
        _shippingRouteLookupService = shippingRouteLookupService ?? throw new ArgumentNullException(nameof(shippingRouteLookupService));
    }

    public async Task<Result<RuleBasedPredictionResult>> CalculateAsync(
        WhatIfPredictionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (status, context) = await _contextBuilder.BuildAsync(request, cancellationToken);
        if (status != DataSufficiency.Sufficient || context is null)
        {
            return Result<RuleBasedPredictionResult>.Failure(InsufficientDataError);
        }

        var engineResult = _predictionEngine.Run(context);
        if (!engineResult.Success)
        {
            return Result<RuleBasedPredictionResult>.Failure(
                new Error(
                    "RuleEngine.Failed",
                    "Order failed rule validation.",
                    ErrorType.Validation));
        }

        var criticalPathOutcome = _criticalPathCalculator.Calculate(engineResult.Context);
        if (criticalPathOutcome.Status != CriticalPathStatus.Success)
        {
            return _resultMapper.Map(context.OrderInput.OrderReference, engineResult, criticalPathOutcome);
        }

        long? shippingDurationMinutes = null;
        var references = _shippingReferenceResolver.Resolve(request.LocationReference);
        if (references is not null)
        {
            var route = await _shippingRouteLookupService.GetRouteAsync(
                references.OriginReference,
                references.DestinationReference,
                references.ShippingProfileReference,
                cancellationToken);
            if (route is ShippingRouteLookupResult.Found found)
            {
                shippingDurationMinutes = found.ShippingDurationMinutes;
            }
        }

        return _resultMapper.Map(
            context.OrderInput.OrderReference,
            engineResult,
            criticalPathOutcome,
            shippingDurationMinutes);
    }
}
