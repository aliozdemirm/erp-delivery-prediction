using App.Application.Contracts.Configuration;

namespace App.Application.Prediction.Resolvers;

public interface IShippingResolver
{
    FallbackResult<TimeSpan?> ResolveShippingDuration(
        long? actualShippingDurationMinutes,
        MvpAssumptionsOptions options);
}
