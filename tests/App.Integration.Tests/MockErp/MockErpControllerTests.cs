using App.Application.Contracts.Erp;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;
using MockErp.Api.Controllers;
using MockErp.Api.Data;
using MockErp.Api.Models;

namespace App.Integration.Tests.MockErp;

public sealed class MockErpControllerTests
{
    private readonly MockErpDataStore _store = new(Path.Combine(
        AppContext.BaseDirectory,
        "Data",
        "mock-erp-seed.json"));

    [Fact]
    public void WorkCenterModelAndJson_ContainOnlyTheMinimumContract()
    {
        Assert.Equal(
            [nameof(MockErpWorkCenter.WorkCenterRef), nameof(MockErpWorkCenter.Name)],
            typeof(MockErpWorkCenter).GetProperties().Select(property => property.Name));

        var json = JsonSerializer.Serialize(
            new MockErpWorkCenter("WC-ASSEMBLY-01", "Assembly Line 1"),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);
        var propertyNames = document.RootElement
            .EnumerateObject()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(["workCenterRef", "name"], propertyNames);
        Assert.DoesNotContain("workCenterReference", propertyNames);
    }

    [Fact]
    public void RoutingAndOperationModelsAndJson_ContainOnlyTheMinimumContract()
    {
        Assert.Equal(
            [nameof(MockErpRouting.RoutingReference), nameof(MockErpRouting.Operations)],
            typeof(MockErpRouting).GetProperties().Select(property => property.Name));
        Assert.Equal(
            [
                nameof(MockErpOperation.OperationReference),
                nameof(MockErpOperation.OperationSequence),
                nameof(MockErpOperation.WorkCenterReference),
                nameof(MockErpOperation.StandardDurationMinutes),
                nameof(MockErpOperation.PredecessorOperationReferences)
            ],
            typeof(MockErpOperation).GetProperties().Select(property => property.Name));
        Assert.Equal(
            typeof(long),
            typeof(MockErpOperation).GetProperty(nameof(MockErpOperation.StandardDurationMinutes))?.PropertyType);

        var json = JsonSerializer.Serialize(
            new MockErpOperation("OP-10", 10, "WC-ASSEMBLY-01", 30L, []),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.TryGetProperty("standardDurationMinutes", out _));
        Assert.Equal(5, document.RootElement.EnumerateObject().Count());
    }

    [Fact]
    public void GetOrdersReturnsFullRuntimeSeedCollectionAndKnownFirstOrders()
    {
        var result = new OrdersController(_store).GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var orders = Assert.IsAssignableFrom<IReadOnlyList<MockErpOrder>>(ok.Value);
        Assert.Equal(1000, orders.Count);
        Assert.Equal(["SO00001", "SO00002", "SO00003"], orders.Take(3).Select(order => order.Id));
    }

    [Theory]
    [InlineData("SO00001", true)]
    [InlineData("ORD-UNKNOWN", false)]
    public void GetOrderReturnsExpectedStatus(string id, bool exists)
    {
        var result = new OrdersController(_store).GetById(id);

        Assert.Equal(exists, result.Result is OkObjectResult);
        Assert.Equal(!exists, result.Result is NotFoundResult);
    }

    [Fact]
    public void GetProductsReturnsFullRuntimeSeedCollection()
    {
        var result = new ProductsController(_store).GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var products = Assert.IsAssignableFrom<IReadOnlyList<MockErpProduct>>(ok.Value);
        Assert.Equal(4, products.Count);
        Assert.Equal(["P001", "P002", "P003", "P004"], products.Select(product => product.Id));
    }

    [Theory]
    [InlineData("P002", true)]
    [InlineData("PROD-UNKNOWN", false)]
    public void GetProductReturnsExpectedStatus(string id, bool exists)
    {
        var result = new ProductsController(_store).GetById(id);

        Assert.Equal(exists, result.Result is OkObjectResult);
        Assert.Equal(!exists, result.Result is NotFoundResult);
    }

    [Fact]
    public void GetP002BomReturnsFullRuntimeSeedLines()
    {
        var result = new ProductsController(_store).GetBom("P002");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var lines = Assert.IsAssignableFrom<IReadOnlyList<MockErpBomLine>>(ok.Value);
        Assert.Equal(11, lines.Count);
        Assert.Equal("MAT-AHSAP-OTURAK", lines[0].ComponentId);
        Assert.Contains(lines, line => line.ComponentId == "MAT-AHSAP-OTURAK");
    }

    [Fact]
    public void StockEndpointReturnsP002QuantityAndUnknownEmptyCollection()
    {
        var controller = new StockLevelsController(_store);

        var matching = Assert.IsType<OkObjectResult>(
            controller.Get(["P002"], default).Result);
        var stock = Assert.Single(
            Assert.IsAssignableFrom<IReadOnlyList<MockErpStockLevel>>(matching.Value));
        Assert.Equal(500m, stock.AvailableQuantity);

        var empty = Assert.IsType<OkObjectResult>(
            controller.Get(["UNKNOWN"], default).Result);
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<MockErpStockLevel>>(empty.Value));
    }

    [Fact]
    public void IntentionallyEmptyPurchaseOrderAndWorkOrderEndpointsReturnOkWithEmptyCollections()
    {
        var purchaseResult = Assert.IsType<OkObjectResult>(
            new OpenPurchaseOrdersController(_store).Get(["P002"], default).Result);
        var purchaseOrders = Assert.IsAssignableFrom<IReadOnlyList<MockErpOpenPurchaseOrder>>(
            purchaseResult.Value);
        Assert.NotNull(purchaseOrders);
        Assert.Empty(purchaseOrders);

        var workResult = Assert.IsType<OkObjectResult>(
            new WorkOrdersController(_store)
                .Get("SO00001", ["P002"], default).Result);
        var workOrders = Assert.IsAssignableFrom<IReadOnlyList<MockErpWorkOrder>>(workResult.Value);
        Assert.NotNull(workOrders);
        Assert.Empty(workOrders);
    }

    [Fact]
    public void IntentionallyEmptyCapacityEndpointReturnsOkAndRejectsInvalidRange()
    {
        var controller = new CapacityCalendarController(_store);
        var start = DateTimeOffset.Parse("2026-08-03T00:00:00+03:00");
        var end = DateTimeOffset.Parse("2026-08-04T23:59:59+03:00");

        var valid = Assert.IsType<OkObjectResult>(
            controller.Get(["WC-QUALITY-01"], start, end, default).Result);
        var calendar = Assert.IsType<MockErpCapacityAndCalendar>(valid.Value);
        Assert.NotNull(calendar.WorkCenters);
        Assert.Empty(calendar.WorkCenters);
        Assert.NotNull(calendar.Shifts);
        Assert.Empty(calendar.Shifts);
        Assert.NotNull(calendar.Holidays);
        Assert.Empty(calendar.Holidays);
        Assert.NotNull(calendar.PlannedDowntimes);
        Assert.Empty(calendar.PlannedDowntimes);

        Assert.IsType<ObjectResult>(
            controller.Get(["WC-QUALITY-01"], end, start, default).Result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public void IntentionallyEmptyShippingEndpointReturnsNotFound()
    {
        var controller = new ShippingDurationsController(_store);

        Assert.IsType<NotFoundResult>(
            controller.Get("WH-IST-01", "CUSTOMER-ANK-01", "STANDARD", default).Result);
        Assert.IsType<NotFoundResult>(
            controller.Get("UNKNOWN", "UNKNOWN", "UNKNOWN", default).Result);
    }

    [Theory]
    [InlineData(typeof(StockLevelsController), "api/stock-levels")]
    [InlineData(typeof(OpenPurchaseOrdersController), "api/open-purchase-orders")]
    [InlineData(typeof(WorkOrdersController), "api/work-orders")]
    [InlineData(typeof(CapacityCalendarController), "api/capacity-calendar")]
    [InlineData(typeof(ShippingDurationsController), "api/shipping-durations")]
    public void NewControllersExposeOnlyGetActions(Type controllerType, string expectedRoute)
    {
        var route = Assert.Single(
            controllerType.GetCustomAttributes(typeof(RouteAttribute), false)
                .Cast<RouteAttribute>());
        Assert.Equal(expectedRoute, route.Template);

        var actions = controllerType.GetMethods()
            .Where(method => method.DeclaringType == controllerType)
            .ToArray();
        Assert.Single(actions);
        var httpMethodAttributes = actions[0].GetCustomAttributes(true)
            .OfType<HttpMethodAttribute>()
            .ToArray();
        var get = Assert.Single(httpMethodAttributes);
        Assert.Equal(["GET"], get.HttpMethods);
    }

    [Theory]
    [InlineData(typeof(MockErpStockLevel), typeof(StockLevelReadDto))]
    [InlineData(typeof(MockErpOpenPurchaseOrder), typeof(OpenPurchaseOrderReadDto))]
    [InlineData(typeof(MockErpOperation), typeof(OperationReadDto))]
    [InlineData(typeof(MockErpWorkCenter), typeof(WorkCenterReadDto))]
    [InlineData(typeof(MockErpWorkingShift), typeof(WorkingShiftReadDto))]
    [InlineData(typeof(MockErpHoliday), typeof(HolidayReadDto))]
    [InlineData(typeof(MockErpPlannedDowntime), typeof(PlannedDowntimeReadDto))]
    [InlineData(typeof(MockErpShippingRoute), typeof(ShippingDurationReadDto))]
    public void ResponseModelsContainApplicationReadDtoProperties(
        Type responseType,
        Type applicationDtoType)
    {
        var responseProperties = responseType.GetProperties()
            .ToDictionary(property => property.Name, property => property.PropertyType);

        foreach (var expected in applicationDtoType.GetProperties())
        {
            Assert.True(
                responseProperties.TryGetValue(expected.Name, out var actualType),
                $"{responseType.Name} is missing {expected.Name}.");
            Assert.Equal(expected.PropertyType, actualType);
        }
    }

    [Fact]
    public void AggregateResponseModelsContainNestedApplicationReadDtoInformation()
    {
        Assert.Equal(
            typeof(MockErpRouting),
            typeof(MockErpWorkOrder).GetProperty(nameof(MockErpWorkOrder.Routing))?.PropertyType);
        Assert.Equal(
            typeof(IReadOnlyList<MockErpOperation>),
            typeof(MockErpRouting).GetProperty(nameof(MockErpRouting.Operations))?.PropertyType);

        var capacityProperties = typeof(MockErpCapacityAndCalendar).GetProperties()
            .Select(property => property.Name);
        Assert.Equal(
            typeof(CapacityAndCalendarReadDto).GetProperties().Select(property => property.Name),
            capacityProperties);
    }
}
