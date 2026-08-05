using App.Application.Contracts.Prediction;

namespace App.Application.Tests.Contracts.Prediction;

public sealed class PredictionRequestValidationTests
{
    private readonly PredictionRequestValidator _validator = new();

    [Fact]
    public void OrderReferenceRequest_WithValidValue_PassesValidation()
    {
        PredictionRequest request = new OrderReferencePredictionRequest { OrderReference = "ORD-1001" };

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void OrderReferenceRequest_WithMissingValue_FailsValidation(string? orderReference)
    {
        var request = new OrderReferencePredictionRequest { OrderReference = orderReference! };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.OrderReference));
    }

    [Fact]
    public void WhatIfRequest_WithValidValues_PassesValidation()
    {
        PredictionRequest request = new WhatIfPredictionRequest
        {
            ProductReference = "PRD-1001",
            Quantity = 25.5m,
            LocationReference = "LOC-IST"
        };

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WhatIfRequest_WithMissingProductReference_FailsValidation(string? productReference)
    {
        var request = ValidWhatIf() with { ProductReference = productReference! };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.ProductReference));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void WhatIfRequest_WithNonPositiveQuantity_FailsValidation(decimal quantity)
    {
        var request = ValidWhatIf() with { Quantity = quantity };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Quantity));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WhatIfRequest_WithMissingLocationReference_FailsValidation(string? locationReference)
    {
        var request = ValidWhatIf() with { LocationReference = locationReference! };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.LocationReference));
    }

    [Fact]
    public void UnsupportedRequestSubtype_FailsValidation()
    {
        PredictionRequest request = new UnsupportedPredictionRequest();

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(PredictionRequest));
    }

    private static WhatIfPredictionRequest ValidWhatIf() => new()
    {
        ProductReference = "PRD-1001",
        Quantity = 25,
        LocationReference = "LOC-IST"
    };

    private sealed record UnsupportedPredictionRequest : PredictionRequest;
}
