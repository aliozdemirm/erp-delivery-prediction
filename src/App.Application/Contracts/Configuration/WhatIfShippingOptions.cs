namespace App.Application.Contracts.Configuration;

public sealed class WhatIfShippingOptions
{
    public const string SectionName = "WhatIfShipping";

    public string? OriginReference { get; set; }
    public string? ShippingProfileReference { get; set; }
    public IDictionary<string, string> DestinationReferences { get; set; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
