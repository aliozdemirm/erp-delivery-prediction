namespace App.Application.Abstractions.Shipping;

public interface IWhatIfShippingReferenceResolver
{
    WhatIfShippingRouteReferences? Resolve(string locationReference);
}

public sealed record WhatIfShippingRouteReferences(
    string OriginReference,
    string DestinationReference,
    string ShippingProfileReference);
