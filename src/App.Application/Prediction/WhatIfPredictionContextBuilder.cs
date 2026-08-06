using App.Application.Abstractions.Erp;
using App.Application.Contracts.Erp;
using App.Application.Contracts.Prediction;
using App.Domain.Abstractions;
using App.Domain.Prediction;

namespace App.Application.Prediction;

public sealed class WhatIfPredictionContextBuilder
{
    private readonly IErpDataProvider _erpDataProvider;
    private readonly IClock _clock;
    private readonly IPredictionContextBuilder _contextBuilder;
    private readonly WhatIfPredictionRequestValidator _validator = new();

    public WhatIfPredictionContextBuilder(
        IErpDataProvider erpDataProvider,
        IClock clock,
        IPredictionContextBuilder contextBuilder)
    {
        _erpDataProvider = erpDataProvider
            ?? throw new ArgumentNullException(nameof(erpDataProvider));

        _clock = clock
            ?? throw new ArgumentNullException(nameof(clock));

        _contextBuilder = contextBuilder
            ?? throw new ArgumentNullException(nameof(contextBuilder));
    }

    public async Task<(DataSufficiency Status, PredictionContext? Context)> BuildAsync(
        WhatIfPredictionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            return (DataSufficiency.InsufficientData, null);
        }

        var product = await _erpDataProvider.GetProductAsync(
            request.ProductReference,
            cancellationToken);

        if (product is null)
        {
            return (DataSufficiency.InsufficientData, null);
        }

        var bomItems = await _erpDataProvider.GetProductBomAsync(
            request.ProductReference,
            cancellationToken);

        var safeBomItems =
            (IReadOnlyList<BomItemReadDto>?)bomItems
            ?? Array.Empty<BomItemReadDto>();

        var productReferences = new[] { request.ProductReference }
            .Concat(safeBomItems.Select(item => item.ComponentProductReference))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var stockLevels = await _erpDataProvider.GetStockLevelsAsync(
            productReferences,
            cancellationToken);

        var safeStockLevels =
            (IReadOnlyList<StockLevelReadDto>?)stockLevels
            ?? Array.Empty<StockLevelReadDto>();

        var now = _clock.UtcNow;
        var syntheticOrderReference = $"WHATIF-{request.ProductReference}";

        var syntheticSnapshot = new ErpBatchSnapshot(
            now,
            new OrderReadDto(
                syntheticOrderReference,
                now,
                null,
                null),
            new[]
            {
                new OrderItemReadDto(
                    syntheticOrderReference,
                    $"{syntheticOrderReference}-L1",
                    request.ProductReference,
                    request.Quantity,
                    product.UnitOfMeasure)
            },
            new[] { product },
            safeBomItems,
            safeStockLevels,
            Array.Empty<OpenPurchaseOrderReadDto>(),
            Array.Empty<WorkOrderReadDto>());

        return _contextBuilder.Build(syntheticSnapshot);
    }
}