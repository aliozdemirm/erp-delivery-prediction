using App.Application.Contracts.Configuration;
using App.Domain.Prediction;

namespace App.Application.Prediction.Resolvers;

public interface IProcurementResolver
{
    FallbackResult<DateTimeOffset> ResolveAvailabilityDate(
        MaterialPurchaseOrder? openPo,
        DateTimeOffset currentTime,
        MvpAssumptionsOptions options);
}
