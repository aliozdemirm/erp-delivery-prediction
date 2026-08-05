using App.Application.Contracts.Configuration;

namespace App.Application.Prediction.Resolvers;

public sealed class ShippingResolver : IShippingResolver
{
    public FallbackResult<TimeSpan?> ResolveShippingDuration(long? actualShippingDurationMinutes, MvpAssumptionsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (actualShippingDurationMinutes.HasValue)
        {
            return new FallbackResult<TimeSpan?>(TimeSpan.FromMinutes(actualShippingDurationMinutes.Value), false, null);
        }

        if (options.Shipping.FallbackDurationMinutes.HasValue)
        {
            return new FallbackResult<TimeSpan?>(TimeSpan.FromMinutes(options.Shipping.FallbackDurationMinutes.Value), true, "Shipping duration not found, using fallback");
        }

        return new FallbackResult<TimeSpan?>(null, true, "No shipping duration and no fallback configured");
    }
}
