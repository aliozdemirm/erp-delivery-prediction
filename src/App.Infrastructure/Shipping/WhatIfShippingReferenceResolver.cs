using App.Application.Abstractions.Shipping;
using App.Application.Contracts.Configuration;
using Microsoft.Extensions.Options;

namespace App.Infrastructure.Shipping;

public sealed class WhatIfShippingReferenceResolver(IOptions<WhatIfShippingOptions> options)
    : IWhatIfShippingReferenceResolver
{
    private static readonly HashSet<string> SupportedLocationKeys =
        new(["istanbul", "ankara", "bursa", "izmir"], StringComparer.OrdinalIgnoreCase);

    public WhatIfShippingRouteReferences? Resolve(string locationReference)
    {
        if (string.IsNullOrWhiteSpace(locationReference) ||
            !SupportedLocationKeys.Contains(locationReference))
        {
            return null;
        }

        var value = options.Value;
        if (string.IsNullOrWhiteSpace(value.OriginReference) ||
            string.IsNullOrWhiteSpace(value.ShippingProfileReference) ||
            !TryGetDestination(value.DestinationReferences, locationReference, out var destination))
        {
            return null;
        }

        return new WhatIfShippingRouteReferences(
            value.OriginReference.Trim(),
            destination.Trim(),
            value.ShippingProfileReference.Trim());
    }

    private static bool TryGetDestination(
        IDictionary<string, string>? destinations,
        string locationReference,
        out string destination)
    {
        destination = string.Empty;
        if (destinations is null)
        {
            return false;
        }

        var match = destinations.FirstOrDefault(pair =>
            string.Equals(pair.Key, locationReference, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(match.Value))
        {
            return false;
        }

        destination = match.Value;
        return true;
    }
}
