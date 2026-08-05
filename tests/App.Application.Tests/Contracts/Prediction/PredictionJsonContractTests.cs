using System.Text.Json;
using App.Application.Contracts.Prediction;

namespace App.Application.Tests.Contracts.Prediction;

public sealed class PredictionJsonContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void OrderReferencePayload_DeserializesToExpectedVariant()
    {
        const string json = """{"requestType":"orderReference","orderReference":"ORD-1001"}""";

        var request = Assert.IsType<OrderReferencePredictionRequest>(
            JsonSerializer.Deserialize<PredictionRequest>(json, JsonOptions));

        Assert.Equal("ORD-1001", request.OrderReference);
    }

    [Fact]
    public void OrderReferenceVariant_RoundTripsWithExactShape()
    {
        PredictionRequest source = new OrderReferencePredictionRequest { OrderReference = "ORD-1001" };

        var json = JsonSerializer.Serialize(source, JsonOptions);
        using var document = JsonDocument.Parse(json);
        var properties = document.RootElement.EnumerateObject().Select(property => property.Name).ToArray();

        Assert.Equal(2, properties.Length);
        Assert.True(properties.ToHashSet().SetEquals(["requestType", "orderReference"]));
        Assert.Equal("orderReference", document.RootElement.GetProperty("requestType").GetString());
        Assert.Equal("ORD-1001", document.RootElement.GetProperty("orderReference").GetString());
        Assert.Equal(source, JsonSerializer.Deserialize<PredictionRequest>(json, JsonOptions));
    }

    [Fact]
    public void WhatIfPayload_DeserializesToExpectedVariant()
    {
        const string json = """{"requestType":"whatIf","productReference":"PRD-1001","quantity":25.5,"locationReference":"LOC-IST"}""";

        var request = Assert.IsType<WhatIfPredictionRequest>(
            JsonSerializer.Deserialize<PredictionRequest>(json, JsonOptions));

        Assert.Equal("PRD-1001", request.ProductReference);
        Assert.Equal(25.5m, request.Quantity);
        Assert.Equal("LOC-IST", request.LocationReference);
    }

    [Fact]
    public void WhatIfVariant_RoundTripsWithExactShape()
    {
        PredictionRequest source = new WhatIfPredictionRequest
        {
            ProductReference = "PRD-1001",
            Quantity = 25.5m,
            LocationReference = "LOC-IST"
        };

        var json = JsonSerializer.Serialize(source, JsonOptions);
        using var document = JsonDocument.Parse(json);
        var properties = document.RootElement.EnumerateObject().Select(property => property.Name).ToArray();

        Assert.Equal(4, properties.Length);
        Assert.True(properties.ToHashSet().SetEquals(
            ["requestType", "productReference", "quantity", "locationReference"]));
        Assert.Equal("whatIf", document.RootElement.GetProperty("requestType").GetString());
        Assert.Equal("PRD-1001", document.RootElement.GetProperty("productReference").GetString());
        Assert.Equal(25.5m, document.RootElement.GetProperty("quantity").GetDecimal());
        Assert.Equal("LOC-IST", document.RootElement.GetProperty("locationReference").GetString());
        Assert.Equal(source, JsonSerializer.Deserialize<PredictionRequest>(json, JsonOptions));
    }

    [Theory]
    [InlineData("""{"orderReference":"ORD-1001"}""")]
    [InlineData("""{"requestType":"unsupported","orderReference":"ORD-1001"}""")]
    public void InvalidDiscriminator_IsRejected(string json)
    {
        var exception = Record.Exception(() => JsonSerializer.Deserialize<PredictionRequest>(json, JsonOptions));

        Assert.True(exception is JsonException or NotSupportedException);
    }

    [Theory]
    [InlineData("""{"requestType":"orderReference"}""")]
    [InlineData("""{"requestType":"whatIf","quantity":1,"locationReference":"LOC-IST"}""")]
    [InlineData("""{"requestType":"whatIf","productReference":"PRD-1001","quantity":1}""")]
    [InlineData("""{"requestType":"whatIf","productReference":"PRD-1001","locationReference":"LOC-IST"}""")]
    public void MissingRequiredProperty_IsRejected(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PredictionRequest>(json, JsonOptions));
    }

    [Fact]
    public void PredictionResponse_UsesExpectedCamelCaseShape()
    {
        var json = JsonSerializer.Serialize(new PredictionResponse(1_440), JsonOptions);
        using var document = JsonDocument.Parse(json);

        var property = Assert.Single(document.RootElement.EnumerateObject());
        Assert.Equal("totalDurationMinutes", property.Name);
        Assert.Equal(1_440, property.Value.GetInt64());
    }
}
