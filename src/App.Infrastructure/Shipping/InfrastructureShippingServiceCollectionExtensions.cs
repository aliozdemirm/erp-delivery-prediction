using App.Application.Abstractions.Shipping;
using Microsoft.Extensions.DependencyInjection;

namespace App.Infrastructure.Shipping;

public static class InfrastructureShippingServiceCollectionExtensions
{
    public static IServiceCollection AddShippingLookup(this IServiceCollection services)
    {
        services.AddTransient<IShippingRouteLookupService, ShippingRouteLookupService>();
        services.AddTransient<IWhatIfShippingReferenceResolver, WhatIfShippingReferenceResolver>();
        return services;
    }
}
