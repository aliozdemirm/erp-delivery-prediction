using App.Api.Configuration;
using App.Api.Prediction.Demo;
using App.Application.Abstractions.Erp;
using App.Application.Erp;
using App.Domain.Abstractions;
using App.Application.Prediction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace App.Api.Tests.Configuration;

public class DemoWorkOrderConfigurationTests
{
    private static ServiceProvider BuildProvider(string? enableSyntheticWorkOrderValue)
    {
        var configValues = new Dictionary<string, string?>();
        if (enableSyntheticWorkOrderValue is not null)
        {
            configValues["Demo:EnableSyntheticWorkOrder"] = enableSyntheticWorkOrderValue;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IErpDataProvider>());
        services.AddSingleton(Mock.Of<IClock>());
        services.AddErpBatchReader();
        services.AddPredictionServices();
        services.AddDemoWorkOrderSupport(configuration);

        return services.BuildServiceProvider();
    }

    [Fact]
    public void FlagNotConfigured_DefaultsToFalse_ResolvesOriginalErpBatchReader()
    {
        using var provider = BuildProvider(enableSyntheticWorkOrderValue: null);

        var reader = provider.GetRequiredService<IErpBatchReader>();

        Assert.IsType<ErpBatchReader>(reader);
    }

    [Fact]
    public void FlagFalse_ResolvesOriginalErpBatchReader()
    {
        using var provider = BuildProvider(enableSyntheticWorkOrderValue: "false");

        var reader = provider.GetRequiredService<IErpBatchReader>();

        Assert.IsType<ErpBatchReader>(reader);
    }

    [Fact]
    public void FlagTrue_ResolvesDemoWorkOrderDecorator()
    {
        using var provider = BuildProvider(enableSyntheticWorkOrderValue: "true");

        var reader = provider.GetRequiredService<IErpBatchReader>();

        Assert.IsType<DemoWorkOrderErpBatchReader>(reader);
    }

    [Theory]
    [InlineData(null, typeof(PredictionContextBuilder))]
    [InlineData("false", typeof(PredictionContextBuilder))]
    [InlineData("true", typeof(DemoWorkOrderPredictionContextBuilder))]
    public void ExistingFlag_ControlsSharedContextEnrichmentRegistration(
        string? flagValue,
        Type expectedType)
    {
        using var provider = BuildProvider(flagValue);

        var builder = provider.GetRequiredService<IPredictionContextBuilder>();

        Assert.IsType(expectedType, builder);
    }
}
