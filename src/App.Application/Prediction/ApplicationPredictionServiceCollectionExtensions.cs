using App.Application.Prediction.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace App.Application.Prediction;

public static class ApplicationPredictionServiceCollectionExtensions
{
    public static IServiceCollection AddPredictionServices(this IServiceCollection services)
    {
        services.AddTransient<PredictionContextBuilder>();
        
        services.AddTransient<ProcurementResolver>();
        services.AddTransient<ShippingResolver>();
        services.AddTransient<CapacityResolver>();

        services.AddTransient<RuleBasedPredictionEngine>();

        return services;
    }
}
