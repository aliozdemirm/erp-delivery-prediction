using App.Application.Contracts.Configuration;
using App.Domain.Prediction;

namespace App.Application.Prediction.Resolvers;

public sealed class ProcurementResolver : IProcurementResolver
{
    public FallbackResult<DateTimeOffset> ResolveAvailabilityDate(MaterialPurchaseOrder? openPo, DateTimeOffset currentTime, MvpAssumptionsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (openPo != null)
        {
            return new FallbackResult<DateTimeOffset>(openPo.ExpectedAvailabilityDateTime, false, null);
        }

        var fallbackDuration = TimeSpan.FromMinutes(options.Procurement.FallbackDurationMinutes);
        var fallbackDate = currentTime.Add(fallbackDuration);

        return new FallbackResult<DateTimeOffset>(fallbackDate, true, "No Open PO found, using fallback lead time");
    }
}
