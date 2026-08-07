using App.Application.Contracts.Configuration;
using App.Infrastructure.Shipping;
using Microsoft.Extensions.Options;

namespace App.Infrastructure.Tests.Shipping;

public sealed class WhatIfShippingReferenceResolverTests
{
    [Theory]
    [InlineData("istanbul", "ERP-IST")]
    [InlineData("ankara", "ERP-ANK")]
    [InlineData("bursa", "ERP-BUR")]
    [InlineData("izmir", "ERP-IZM")]
    public void Resolve_WithCompleteConfiguration_ReturnsConfiguredReferences(string key, string destination)
    {
        var resolver = Create(new WhatIfShippingOptions
        {
            OriginReference = "ERP-ORIGIN",
            ShippingProfileReference = "ERP-PROFILE",
            DestinationReferences = new Dictionary<string, string> { [key] = destination }
        });

        var result = resolver.Resolve(key);

        Assert.NotNull(result);
        Assert.Equal("ERP-ORIGIN", result.OriginReference);
        Assert.Equal(destination, result.DestinationReference);
        Assert.Equal("ERP-PROFILE", result.ShippingProfileReference);
    }

    [Theory]
    [InlineData(null, "PROFILE", "DEST")]
    [InlineData("", "PROFILE", "DEST")]
    [InlineData("ORIGIN", null, "DEST")]
    [InlineData("ORIGIN", "", "DEST")]
    [InlineData("ORIGIN", "PROFILE", null)]
    [InlineData("ORIGIN", "PROFILE", "")]
    public void Resolve_WithIncompleteConfiguration_ReturnsNull(string? origin, string? profile, string? destination)
    {
        var resolver = Create(new WhatIfShippingOptions
        {
            OriginReference = origin,
            ShippingProfileReference = profile,
            DestinationReferences = new Dictionary<string, string> { ["istanbul"] = destination! }
        });

        Assert.Null(resolver.Resolve("istanbul"));
    }

    [Fact]
    public void Resolve_WithUnknownApplicationKey_ReturnsNull()
        => Assert.Null(Create(new WhatIfShippingOptions()).Resolve("unknown"));

    private static WhatIfShippingReferenceResolver Create(WhatIfShippingOptions options)
        => new(Options.Create(options));
}
