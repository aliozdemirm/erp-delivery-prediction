using System.Reflection;
using App.Application.Contracts.Prediction;

namespace App.Application.Tests.Contracts.Prediction;

public sealed class PredictionContractStructureTests
{
    [Fact]
    public void RequestVariants_AreSeparateAndExposeOnlyTheirOwnFields()
    {
        Assert.NotEqual(typeof(OrderReferencePredictionRequest), typeof(WhatIfPredictionRequest));
        Assert.Equal(
            [nameof(OrderReferencePredictionRequest.OrderReference)],
            PublicInstancePropertiesOf<OrderReferencePredictionRequest>());
        Assert.Equal(
            [
                nameof(WhatIfPredictionRequest.ProductReference),
                nameof(WhatIfPredictionRequest.Quantity),
                nameof(WhatIfPredictionRequest.LocationReference)
            ],
            PublicInstancePropertiesOf<WhatIfPredictionRequest>());
        Assert.Equal(typeof(decimal), typeof(WhatIfPredictionRequest).GetProperty(nameof(WhatIfPredictionRequest.Quantity))!.PropertyType);
    }

    [Theory]
    [InlineData(typeof(PredictionRequest))]
    [InlineData(typeof(OrderReferencePredictionRequest))]
    [InlineData(typeof(WhatIfPredictionRequest))]
    [InlineData(typeof(PredictionResponse))]
    public void ContractTypes_ArePublicAndImmutable(Type contractType)
    {
        Assert.True(contractType.IsPublic);

        foreach (var property in contractType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            Assert.True(property.CanRead);
            Assert.True(property.SetMethod is null || IsInitOnly(property.SetMethod));
        }
    }

    [Fact]
    public void PredictionResponse_HasNoIntegrationDependenciesAndIsRequestIndependent()
    {
        var constructor = Assert.Single(typeof(PredictionResponse).GetConstructors());
        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal(typeof(long), parameter.ParameterType);
        Assert.DoesNotContain(
            typeof(PredictionResponse).GetProperties(),
            property => property.PropertyType.Namespace?.StartsWith("App.Integration", StringComparison.Ordinal) == true);
        Assert.False(typeof(PredictionRequest).IsAssignableFrom(typeof(PredictionResponse)));
    }

    private static string[] PublicInstancePropertiesOf<T>() =>
        typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .ToArray();

    private static bool IsInitOnly(MethodInfo setter) =>
        setter.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(System.Runtime.CompilerServices.IsExternalInit));
}
