using App.Application.Abstractions.Erp;
using App.Application.Contracts.Configuration;
using App.Application.Abstractions.Shipping;
using App.Application.Prediction;
using App.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace App.Integration.Tests.DependencyInjection;

public sealed class PredictionServiceRegistrationTests
{
    [Fact]
    public void AddPredictionServices_ResolvesWhatIfPredictionCalculationService()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IErpDataProvider>());
        services.AddSingleton(Mock.Of<IErpBatchReader>());
        services.AddSingleton(Mock.Of<IClock>());
        services.AddSingleton(Mock.Of<IWhatIfShippingReferenceResolver>());
        services.AddSingleton(Mock.Of<IShippingRouteLookupService>());
        services.AddSingleton(new MvpAssumptionsOptions());
        services.AddPredictionServices();

        using var serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        var service = serviceProvider.GetRequiredService<IWhatIfPredictionCalculationService>();

        Assert.IsType<WhatIfPredictionCalculationService>(service);
    }
}
