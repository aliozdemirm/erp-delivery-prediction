using App.Api.Prediction.Demo;
using App.Application.Abstractions.Erp;
using App.Application.Erp;
using App.Application.Prediction.Demo;
using App.Application.Prediction;
using Microsoft.Extensions.Options;

namespace App.Api.Configuration;

/// <summary>
/// Opt-in composition-root registration for the DemoWorkOrderErpBatchReader decorator. Must be
/// called after AddErpBatchReader(); leaves that registration untouched when the flag is off.
/// </summary>
public static class DemoWorkOrderConfigurationExtensions
{
    public static IServiceCollection AddDemoWorkOrderSupport(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<DemoWorkOrderOptions>()
            .Bind(configuration.GetSection(DemoWorkOrderOptions.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<DemoWorkOrderOptions>>().Value);

        var isEnabled = configuration.GetValue<bool>($"{DemoWorkOrderOptions.SectionName}:{nameof(DemoWorkOrderOptions.EnableSyntheticWorkOrder)}");
        if (!isEnabled)
        {
            return services;
        }

        services.AddScoped<ErpBatchReader>();
        services.AddSingleton<DemoWorkOrderSnapshotEnricher>();
        services.AddScoped<IErpBatchReader>(sp => new DemoWorkOrderErpBatchReader(
            sp.GetRequiredService<ErpBatchReader>(),
            sp.GetRequiredService<DemoWorkOrderOptions>(),
            sp.GetRequiredService<DemoWorkOrderSnapshotEnricher>(),
            sp.GetRequiredService<ILogger<DemoWorkOrderErpBatchReader>>()));
        services.AddTransient<IPredictionContextBuilder>(sp => new DemoWorkOrderPredictionContextBuilder(
            sp.GetRequiredService<PredictionContextBuilder>(),
            sp.GetRequiredService<DemoWorkOrderSnapshotEnricher>(),
            sp.GetRequiredService<ILogger<DemoWorkOrderPredictionContextBuilder>>()));

        return services;
    }
}
