using System.Collections;
using System.Text.Json;
using MockErp.Api.Data;
using MockErp.Api.Models;

namespace App.Integration.Tests.MockErp;

public sealed class MockErpDataStoreTests
{
    private static readonly string SeedPath = Path.Combine(
        AppContext.BaseDirectory,
        "Data",
        "mock-erp-seed.json");

    [Fact]
    public void FullRuntimeSeedLoadsExpectedCoreData()
    {
        var store = new MockErpDataStore(SeedPath);

        Assert.Equal(1000, store.GetOrders().Count);

        var order = Assert.IsType<MockErpOrder>(store.GetOrder("SO00001"));
        Assert.Equal("P002", order.ProductId);
        Assert.Equal(16, order.Quantity);
        Assert.Equal(new DateOnly(2026, 7, 2), order.RequestedDeliveryDate);

        var product = Assert.IsType<MockErpProduct>(store.GetProduct("P002"));
        Assert.Equal("Sandalye", product.Name);
        Assert.Equal("Adet", product.Unit);

        var bom = store.GetProductBom("P002");
        Assert.Equal(11, bom.Count);
        Assert.Equal("MAT-AHSAP-OTURAK", bom[0].ComponentId);
        Assert.Contains(bom, line => line.ComponentId == "MAT-AHSAP-OTURAK");
    }

    [Fact]
    public void GetProductsReturnsFullRuntimeSeedCollection()
    {
        var store = new MockErpDataStore(SeedPath);

        var products = store.GetProducts();

        Assert.Equal(4, products.Count);
        Assert.Equal(["P001", "P002", "P003", "P004"], products.Select(product => product.Id));
    }

    [Fact]
    public void RepeatedReadsAreEquivalentAndUnknownIdsReturnNoData()
    {
        var store = new MockErpDataStore(SeedPath);

        Assert.Equal(store.GetOrders(), store.GetOrders());
        Assert.Equal(store.GetProducts(), store.GetProducts());
        Assert.Equal(store.GetProductBom("P002"), store.GetProductBom("P002"));
        Assert.Null(store.GetOrder("ORD-UNKNOWN"));
        Assert.Null(store.GetProduct("PROD-UNKNOWN"));
        Assert.Empty(store.GetProductBom("PROD-UNKNOWN"));
    }

    [Fact]
    public void MissingSeedProducesClearFailure()
    {
        var missingPath = Path.Combine(AppContext.BaseDirectory, "missing-seed.json");

        var exception = Assert.Throws<InvalidOperationException>(
            () => new MockErpDataStore(missingPath));

        Assert.Contains("was not found", exception.Message);
    }

    [Fact]
    public void FullRuntimeSeedContainsRequiredRootsAndExpectedCollectionCounts()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(SeedPath));
        var root = document.RootElement;

        var requiredRoots = new[]
        {
            "orders", "products", "boms", "stockLevels",
            "openPurchaseOrders", "workOrders", "shippingDurations", "capacityCalendar"
        };
        Assert.All(requiredRoots, property => Assert.True(root.TryGetProperty(property, out _), property));

        Assert.Equal(1000, root.GetProperty("orders").GetArrayLength());
        Assert.Equal(4, root.GetProperty("products").GetArrayLength());
        Assert.Equal(4, root.GetProperty("boms").GetArrayLength());
        Assert.Equal(
            51,
            root.GetProperty("boms").EnumerateArray()
                .Sum(bom => bom.GetProperty("lines").GetArrayLength()));
        Assert.Equal(4, root.GetProperty("stockLevels").GetArrayLength());
        Assert.Empty(root.GetProperty("openPurchaseOrders").EnumerateArray());
        Assert.Empty(root.GetProperty("workOrders").EnumerateArray());
        Assert.Empty(root.GetProperty("shippingDurations").EnumerateArray());

        var capacity = root.GetProperty("capacityCalendar");
        Assert.Empty(capacity.GetProperty("workCenters").EnumerateArray());
        Assert.Empty(capacity.GetProperty("shifts").EnumerateArray());
        Assert.Empty(capacity.GetProperty("holidays").EnumerateArray());
        Assert.Empty(capacity.GetProperty("plannedDowntimes").EnumerateArray());
    }

    [Fact]
    public void FullRuntimeSeedStockForP002HasExpectedQuantitiesAndNullLocation()
    {
        var store = new MockErpDataStore(SeedPath);

        var result = store.GetStockLevels(["P002"]);

        var stock = Assert.Single(result);
        Assert.Equal(500m, stock.OnHandQuantity);
        Assert.Equal(0m, stock.ReservedQuantity);
        Assert.Equal(500m, stock.AvailableQuantity);
        Assert.Null(stock.LocationReference);
        Assert.Empty(store.GetStockLevels(["UNKNOWN"]));
    }

    [Fact]
    public void IntentionallyEmptyOpenPurchaseOrdersReturnNonNullEmptyCollection()
    {
        var store = new MockErpDataStore(SeedPath);

        var result = store.GetOpenPurchaseOrders(["P002"]);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void IntentionallyEmptyWorkOrdersReturnNonNullEmptyCollection()
    {
        var store = new MockErpDataStore(SeedPath);

        var result = store.GetWorkOrders("SO00001", ["P002"]);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void IntentionallyEmptyCapacityCalendarPreservesRangeAndReturnsNonNullEmptyChildren()
    {
        var store = new MockErpDataStore(SeedPath);
        var rangeStart = DateTimeOffset.Parse("2026-08-04T00:00:00+03:00");
        var rangeEnd = DateTimeOffset.Parse("2026-08-05T23:59:59+03:00");

        var result = store.GetCapacityAndCalendar(
            ["WC-ASSEMBLY-01"],
            rangeStart,
            rangeEnd);

        Assert.Equal(rangeStart, result.RangeStart);
        Assert.Equal(rangeEnd, result.RangeEnd);
        Assert.NotNull(result.WorkCenters);
        Assert.Empty(result.WorkCenters);
        Assert.NotNull(result.Shifts);
        Assert.Empty(result.Shifts);
        Assert.NotNull(result.Holidays);
        Assert.Empty(result.Holidays);
        Assert.NotNull(result.PlannedDowntimes);
        Assert.Empty(result.PlannedDowntimes);
    }

    [Fact]
    public void IntentionallyEmptyShippingDurationsReturnNull()
    {
        var store = new MockErpDataStore(SeedPath);

        var duration = store.GetShippingDuration(
            "WH-IST-01",
            "CUSTOMER-ANK-01",
            "STANDARD");

        Assert.Null(duration);
        Assert.Null(store.GetShippingDuration("UNKNOWN", "UNKNOWN", "UNKNOWN"));
    }

    [Fact]
    public void RepeatedReadsAreDeterministicAndReturnedCollectionsAreReadOnly()
    {
        var store = new MockErpDataStore(SeedPath);
        var first = store.GetStockLevels(["P002"]);
        var second = store.GetStockLevels(["P002"]);
        var bom = store.GetProductBom("P002");

        Assert.Equal(first, second);
        Assert.Throws<NotSupportedException>(
            () => ((IList)first).Add(new MockErpStockLevel("X", null, 0, 0, 0)));
        Assert.Throws<NotSupportedException>(
            () => ((IList)bom).Clear());
        Assert.Single(store.GetStockLevels(["P002"]));
        Assert.Equal(11, store.GetProductBom("P002").Count);
    }

    [Fact]
    public void ValidWorkCenterMasterDataRoundTripsThroughTheStore()
    {
        var seedPath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "MockErp",
            "WorkCenterContract",
            "mock-erp-seed.json");

        var fixtureJson = File.ReadAllText(seedPath);
        Assert.Contains("\"workCenterRef\"", fixtureJson);
        Assert.Contains("\"name\"", fixtureJson);
        Assert.DoesNotContain("workCenterReference", fixtureJson);

        var store = new MockErpDataStore(seedPath);
        var result = store.GetCapacityAndCalendar(
            ["WC-ASSEMBLY-01"],
            DateTimeOffset.Parse("2026-08-01T00:00:00+03:00"),
            DateTimeOffset.Parse("2026-08-31T23:59:59+03:00"));

        var workCenter = Assert.Single(result.WorkCenters);
        Assert.Equal("WC-ASSEMBLY-01", workCenter.WorkCenterRef);
        Assert.Equal("Assembly Line 1", workCenter.Name);
    }

    [Fact]
    public void DuplicateWorkCenterReferencesFailFast()
    {
        var seedPath = WriteTempSeedWithWorkCenters(
            """
            {
              "workCenterRef": "WC-ASSEMBLY-01",
              "name": "Assembly Line 1"
            },
            {
              "workCenterRef": "WC-ASSEMBLY-01",
              "name": "Assembly Line 1 (duplicate)"
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(
            () => new MockErpDataStore(seedPath));

        Assert.Contains("duplicate work center", exception.Message);
        Assert.Contains("WC-ASSEMBLY-01", exception.Message);
    }

    private static string WriteTempSeedWithWorkCenters(string workCentersJsonArrayContent)
    {
        var json = $$"""
            {
              "orders": [],
              "products": [],
              "boms": [],
              "stockLevels": [],
              "openPurchaseOrders": [],
              "workOrders": [],
              "capacityCalendar": {
                "workCenters": [ {{workCentersJsonArrayContent}} ],
                "shifts": [],
                "holidays": [],
                "plannedDowntimes": []
              },
              "shippingDurations": []
            }
            """;

        var path = Path.Combine(Path.GetTempPath(), $"mock-erp-seed-workcenter-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void RoutingContractFixtureLoadsValidAndEmptyRoutings()
    {
        var seedPath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "MockErp",
            "RoutingContract",
            "mock-erp-seed.json");

        var store = new MockErpDataStore(seedPath);
        var workOrders = store.GetWorkOrders("SO-1", ["P-1"]);

        Assert.Equal(2, workOrders.Count);
        var populated = Assert.Single(workOrders, item => item.WorkOrderReference == "WO-ROUTING-01");
        Assert.Equal("ROUTE-P-1-STD", populated.Routing.RoutingReference);
        Assert.Equal(2_147_483_648L, populated.Routing.Operations[0].StandardDurationMinutes);
        Assert.Equal("OP-10", Assert.Single(populated.Routing.Operations[1].PredecessorOperationReferences));
        Assert.Empty(Assert.Single(workOrders, item => item.WorkOrderReference == "WO-EMPTY-ROUTING").Routing.Operations);
    }

    [Fact]
    public void UnknownWorkCenterReferenceStillFailsAtTheSeedBoundary()
    {
        var seedPath = WriteTempSeedWithRouting(
            """
            {
              "operationReference": "OP-10",
              "operationSequence": 10,
              "workCenterReference": "WC-UNKNOWN",
              "standardDurationMinutes": 30,
              "predecessorOperationReferences": []
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() => new MockErpDataStore(seedPath));

        Assert.Contains("unknown work center", exception.Message);
        Assert.Contains("WC-UNKNOWN", exception.Message);
    }

    private static string WriteTempSeedWithRouting(string operationsJson)
    {
        var json = $$"""
            {
              "orders": [],
              "products": [],
              "boms": [],
              "stockLevels": [],
              "openPurchaseOrders": [],
              "workOrders": [{
                "workOrderReference": "WO-1",
                "orderReference": "SO-1",
                "productReference": "P-1",
                "status": "Released",
                "routing": {
                  "routingReference": "ROUTE-P-1-STD",
                  "operations": [{{operationsJson}}]
                }
              }],
              "capacityCalendar": {
                "workCenters": [{ "workCenterRef": "WC-ASSEMBLY-01", "name": "Assembly Line 1" }],
                "shifts": [],
                "holidays": [],
                "plannedDowntimes": []
              },
              "shippingDurations": []
            }
            """;

        var path = Path.Combine(Path.GetTempPath(), $"mock-erp-seed-routing-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }
}
