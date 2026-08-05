namespace App.Application.Contracts.Prediction;

public sealed record PredictionValidationError(string PropertyName, string ErrorMessage);

public sealed record PredictionValidationResult(IReadOnlyList<PredictionValidationError> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

public sealed class OrderReferencePredictionRequestValidator
{
    public PredictionValidationResult Validate(OrderReferencePredictionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return string.IsNullOrWhiteSpace(request.OrderReference)
            ? Invalid(nameof(request.OrderReference), "Order reference is required.")
            : Valid();
    }

    private static PredictionValidationResult Valid() => new([]);

    private static PredictionValidationResult Invalid(string propertyName, string message) =>
        new([new PredictionValidationError(propertyName, message)]);
}

public sealed class WhatIfPredictionRequestValidator
{
    public PredictionValidationResult Validate(WhatIfPredictionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<PredictionValidationError>();

        if (string.IsNullOrWhiteSpace(request.ProductReference))
        {
            errors.Add(new(nameof(request.ProductReference), "Product reference is required."));
        }

        if (request.Quantity <= 0)
        {
            errors.Add(new(nameof(request.Quantity), "Quantity must be greater than zero."));
        }

        if (string.IsNullOrWhiteSpace(request.LocationReference))
        {
            errors.Add(new(nameof(request.LocationReference), "Location reference is required."));
        }

        return new(errors.AsReadOnly());
    }
}

public sealed class PredictionRequestValidator
{
    private readonly OrderReferencePredictionRequestValidator _orderReferenceValidator = new();
    private readonly WhatIfPredictionRequestValidator _whatIfValidator = new();

    public PredictionValidationResult Validate(PredictionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request switch
        {
            OrderReferencePredictionRequest orderReference => _orderReferenceValidator.Validate(orderReference),
            WhatIfPredictionRequest whatIf => _whatIfValidator.Validate(whatIf),
            _ => new([new PredictionValidationError(nameof(PredictionRequest), "Unsupported prediction request type.")])
        };
    }
}
