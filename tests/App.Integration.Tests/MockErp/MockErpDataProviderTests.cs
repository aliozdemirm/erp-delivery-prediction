using System.Net;
using System.Text;
using System.Text.Json;
using App.Application.Abstractions.Erp;
using App.Application.IntegrationLogging;
using App.Integration.MockErp;
using App.Integration.Models;

namespace App.Integration.Tests.MockErp;

public sealed class MockErpDataProviderTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Provider_ImplementsExactPort()
    {
        Assert.True(typeof(IErpDataProvider).IsAssignableFrom(typeof(MockErpDataProvider)));
        Assert.Equal(11, typeof(IErpDataProvider).GetMethods().Length);
    }

    [Fact]
    public void MapWorkCenterReadModel_ProjectsMasterDataFieldsInIsolation()
    {
        var transport = new MockErpWorkCenter("WC&1", "Assembly Line 1");

        var readModel = MockErpDataProvider.MapWorkCenterReadModel(transport);

        Assert.Equal("WC&1", readModel.WorkCenterRef);
        Assert.Equal("Assembly Line 1", readModel.Name);
    }

    [Fact]
    public void WorkCenterTransportAndReadModel_ContainOnlyTheMinimumContract()
    {
        var expectedProperties = new[] { "WorkCenterRef", "Name" };

        Assert.Equal(
            expectedProperties,
            typeof(MockErpWorkCenter).GetProperties().Select(property => property.Name));
        Assert.Equal(
            expectedProperties,
            typeof(WorkCenterReadModel).GetProperties().Select(property => property.Name));
    }

    [Fact]
    public void MapRoutingReadModel_PreservesTheMinimumContractAndLongDuration()
    {
        var source = new MockErpRouting(
            "ROUTE-P1-STD",
            [new MockErpOperation("OP-20", 20, "WC&1", 2_147_483_648L, ["OP-10"])]);

        var result = MockErpDataProvider.MapRoutingReadModel(source);
        var operation = Assert.Single(result.Operations);

        Assert.Equal("ROUTE-P1-STD", result.RoutingReference);
        Assert.Equal("OP-20", operation.OperationReference);
        Assert.Equal(20, operation.OperationSequence);
        Assert.Equal("WC&1", operation.WorkCenterReference);
        Assert.Equal(2_147_483_648L, operation.StandardDurationMinutes);
        Assert.Equal("OP-10", Assert.Single(operation.PredecessorOperationReferences));
    }

    [Fact]
    public void RoutingReadModels_ContainOnlyTheMinimumContract()
    {
        Assert.Equal(
            [nameof(RoutingReadModel.RoutingReference), nameof(RoutingReadModel.Operations)],
            typeof(RoutingReadModel).GetProperties().Select(property => property.Name));
        Assert.Equal(
            [
                nameof(OperationReadModel.OperationReference),
                nameof(OperationReadModel.OperationSequence),
                nameof(OperationReadModel.WorkCenterReference),
                nameof(OperationReadModel.StandardDurationMinutes),
                nameof(OperationReadModel.PredecessorOperationReferences)
            ],
            typeof(OperationReadModel).GetProperties().Select(property => property.Name));
        Assert.Equal(
            typeof(long),
            typeof(OperationReadModel).GetProperty(nameof(OperationReadModel.StandardDurationMinutes))?.PropertyType);
    }

    [Fact]
    public void MapShippingRouteReadModel_PreservesMinimumContract()
    {
        var source = new MockErpShippingRoute("WH-IST", "CUSTOMER-ANK", "STANDARD", 2_147_483_648L);

        var result = MockErpDataProvider.MapShippingRouteReadModel(source);

        Assert.Equal("WH-IST", result.OriginReference);
        Assert.Equal("CUSTOMER-ANK", result.DestinationReference);
        Assert.Equal("STANDARD", result.ShippingProfileReference);
        Assert.Equal(2_147_483_648L, result.ShippingDurationMinutes);
        Assert.Equal(
            ["OriginReference", "DestinationReference", "ShippingProfileReference", "ShippingDurationMinutes"],
            result.GetType().GetProperties().Select(property => property.Name));
    }

    [Fact]
    public async Task OrderItems_PerformsOrderAndProductGets_AndAppliesApprovedMapping()
    {
        var handler = new RecordingHandler(
            Json(new { id = "SO/1", productId = "P 1", quantity = 7, requestedDeliveryDate = "2026-08-03" }),
            Json(new { id = "P 1", name = "Widget", unit = "EA" }));
        var (provider, logs) = Create(handler);

        var result = await provider.GetOrderItemsAsync("SO/1", default);

        var item = Assert.Single(result);
        Assert.Equal("SO/1-L1", item.LineReference);
        Assert.Equal("SO/1", item.OrderReference);
        Assert.Equal("P 1", item.ProductReference);
        Assert.Equal(7m, item.OrderedQuantity);
        Assert.Equal("EA", item.UnitOfMeasure);
        Assert.Equal(new[] { "/api/orders/SO%2F1", "/api/products/P%201" }, handler.Requests.Select(x => x.Uri.AbsolutePath));
        Assert.All(handler.Requests, request => { Assert.Equal(HttpMethod.Get, request.Method); Assert.False(request.HasBody); });
        Assert.Equal(2, logs.Requests.Count);
    }

    [Fact]
    public async Task Order_MapsDateOnlyToUtcStartOfDay()
    {
        var (provider, _) = Create(new RecordingHandler(Json(new { id = "SO-1", productId = "P-1", quantity = 2, requestedDeliveryDate = "2026-12-25" })));

        var order = await provider.GetOrderAsync("SO-1", default);
        Assert.NotNull(order);

        Assert.Equal(new DateTimeOffset(2026, 12, 25, 0, 0, 0, TimeSpan.Zero), order!.RequestedDeliveryDateTime);
        Assert.Equal(TimeSpan.Zero, order.RequestedDeliveryDateTime.Offset);
    }

    [Fact]
    public async Task OrderSummaries_MapsAllOrders_WithASingleBulkRequest()
    {
        var handler = new RecordingHandler(Json(new object[]
        {
            new { id = "SO-1", productId = "P-1", quantity = 2, requestedDeliveryDate = "2026-12-25" },
            new { id = "SO-2", productId = "P-2", quantity = 5, requestedDeliveryDate = "2026-01-01" },
        }));
        var (provider, _) = Create(handler);

        var summaries = await provider.GetOrderSummariesAsync(default);

        Assert.Equal(2, summaries.Count);
        Assert.Equal("SO-1", summaries[0].OrderReference);
        Assert.Equal("P-1", summaries[0].ProductReference);
        Assert.Equal(2m, summaries[0].Quantity);
        Assert.Equal(new DateTimeOffset(2026, 12, 25, 0, 0, 0, TimeSpan.Zero), summaries[0].RequestedDeliveryDateTime);
        Assert.Equal("SO-2", summaries[1].OrderReference);
        Assert.Single(handler.Requests);
        Assert.Equal("/api/orders", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task Products_MapsAllProducts_WithASingleBulkRequest()
    {
        var handler = new RecordingHandler(Json(new object[]
        {
            new { id = "P1", name = "Widget", unit = "EA" },
            new { id = "P2", name = "Gadget", unit = "KG" },
        }));
        var (provider, _) = Create(handler);

        var products = await provider.GetProductsAsync(default);

        Assert.Equal(2, products.Count);
        Assert.Equal("P1", products[0].ProductReference);
        Assert.Null(products[0].PlanningClassification);
        Assert.Equal("EA", products[0].UnitOfMeasure);
        Assert.Equal("Widget", products[0].Name);
        Assert.Equal("P2", products[1].ProductReference);
        Assert.Equal("Gadget", products[1].Name);
        Assert.Single(handler.Requests);
        Assert.Equal("/api/products", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task AllDirectEndpoints_UseActualRoutesQueries_AndMapNestedValues()
    {
        var start = new DateTimeOffset(2026, 8, 1, 9, 15, 0, TimeSpan.FromHours(3));
        var end = start.AddDays(1);
        var handler = new RecordingHandler(
            Json(new { id = "P/1", name = "Product", unit = "KG" }),
            Json(new[] { new { componentId = "C-1", description = "Part", quantity = 1.25m, unit = "KG" } }),
            Json(new[] { new { productReference = "P/1", locationReference = "L1", onHandQuantity = 10.5m, reservedQuantity = 2.25m, availableQuantity = 8.25m } }),
            Json(new[] { new { purchaseOrderReference = "PO1", productReference = "P/1", openQuantity = 3.75m, expectedAvailabilityDateTime = start, supplierLeadTimeMinutes = 90L, status = "Open" } }),
            Json(new[] { new { workOrderReference = "WO1", orderReference = "SO 1", productReference = "P/1", status = "Open", routing = new { routingReference = "ROUTE-P1-STD", operations = new[] { new { operationReference = "OP1", operationSequence = 10, workCenterReference = "WC&1", standardDurationMinutes = 2_147_483_648L, predecessorOperationReferences = new[] { "OP0" } } } } } }),
            Json(new { rangeStart = start, rangeEnd = end, workCenters = new[] { new { workCenterRef = "WC&1", name = "Assembly Line 1" } }, shifts = new[] { new { workCenterReference = "WC&1", start, end } }, holidays = new[] { new { date = "2026-08-02", workCenterReference = (string?)null } }, plannedDowntimes = new[] { new { workCenterReference = "WC&1", start, end, plannedDowntimeMinutes = 60L } } }),
            Json(new { originReference = "TR IST", destinationReference = "DE/BER", shippingProfileReference = "AIR&FAST", shippingDurationMinutes = 1440L }));
        var (provider, _) = Create(handler);

        var product = await provider.GetProductAsync("P/1", default);
        Assert.Equal("KG", product!.UnitOfMeasure);
        Assert.Equal("Product", product.Name);
        Assert.Equal(1.25m, Assert.Single(await provider.GetProductBomAsync("P/1", default)).RequiredQuantityPerParentUnit);
        Assert.Equal(8.25m, Assert.Single(await provider.GetStockLevelsAsync(["P/1", "P 2"], default)).AvailableQuantity);
        Assert.Equal(90, Assert.Single(await provider.GetOpenPurchaseOrdersAsync(["P/1"], default)).SupplierLeadTimeMinutes);
        var workOrder = Assert.Single(await provider.GetWorkOrdersAsync("SO 1", ["P/1", "P 2"], default));
        var operation = Assert.Single(workOrder.Routing.Operations);
        Assert.Equal(2_147_483_648L, operation.StandardDurationMinutes);
        Assert.Equal("OP0", Assert.Single(operation.PredecessorOperationReferences));
        Assert.Equal("ROUTE-P1-STD", workOrder.Routing.RoutingReference);
        var capacity = await provider.GetCapacityAndCalendarAsync(["WC&1", "WC 2"], start, end, default);
        var workCenter = Assert.Single(capacity.WorkCenters);
        Assert.Equal("WC&1", workCenter.WorkCenterRef);
        Assert.Equal("Assembly Line 1", workCenter.Name);
        Assert.Equal(new DateOnly(2026, 8, 2), Assert.Single(capacity.Holidays).Date);
        Assert.Equal(1440, (await provider.GetShippingDurationAsync("TR IST", "DE/BER", "AIR&FAST", default))!.ShippingDurationMinutes);

        Assert.Equal(7, handler.Requests.Count);
        Assert.All(handler.Requests, x => { Assert.Equal(HttpMethod.Get, x.Method); Assert.False(x.HasBody); });
        Assert.Equal("?productReferences=P%2F1&productReferences=P%202", handler.Requests[2].Uri.Query);
        Assert.Contains("orderReference=SO%201", handler.Requests[4].Uri.Query);
        Assert.Contains("productReferences=P%2F1&productReferences=P%202", handler.Requests[4].Uri.Query);
        Assert.Contains("workCenterReferences=WC%261&workCenterReferences=WC%202", handler.Requests[5].Uri.Query);
        Assert.Contains("rangeStart=2026-08-01T09%3A15%3A00.0000000%2B03%3A00", handler.Requests[5].Uri.Query);
        Assert.Equal("?originReference=TR%20IST&destinationReference=DE%2FBER&shippingProfileReference=AIR%26FAST", handler.Requests[6].Uri.Query);
    }

    [Fact]
    public async Task NullableAndCollectionSemantics_Handle404And204WithoutRetry()
    {
        var handler = new RecordingHandler(Response(HttpStatusCode.NotFound), Response(HttpStatusCode.NoContent), Response(HttpStatusCode.NotFound));
        var (provider, logs) = Create(handler);

        Assert.Null(await provider.GetOrderAsync("missing", default));
        Assert.Empty(await provider.GetStockLevelsAsync(["P1"], default));
        Assert.Null(await provider.GetShippingDurationAsync("A", "B", "C", default));
        Assert.Equal(3, handler.Requests.Count);
        Assert.All(logs.Requests, x => Assert.True(x.IsSuccess));
    }

    [Fact]
    public async Task MissingOrderReturnsEmptyItems_AndMissingReferencedProductFailsSafely()
    {
        var missingOrder = new RecordingHandler(Response(HttpStatusCode.NotFound));
        var (first, _) = Create(missingOrder);
        Assert.Empty(await first.GetOrderItemsAsync("missing", default));

        var missingProduct = new RecordingHandler(Json(new { id = "SO1", productId = "P1", quantity = 1, requestedDeliveryDate = "2026-08-01" }), Response(HttpStatusCode.NotFound));
        var (second, _) = Create(missingProduct);
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => second.GetOrderItemsAsync("SO1", default));
        Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
        Assert.Equal(2, missingProduct.Requests.Count);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task NonTransientStatus_IsNotRetried(HttpStatusCode status)
    {
        var handler = new RecordingHandler(Response(status));
        var (provider, logs) = Create(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.GetProductBomAsync("P1", default));

        Assert.Single(handler.Requests);
        var log = Assert.Single(logs.Requests);
        Assert.False(log.IsSuccess);
        Assert.Equal((int)status, log.StatusCode);
        Assert.True(log.DurationMs >= 0);
    }

    [Fact]
    public async Task TransientStatuses_RetryUpToMaximum_AndLogEveryAttempt()
    {
        var handler = new RecordingHandler(Response(HttpStatusCode.ServiceUnavailable), Response(HttpStatusCode.BadGateway), Json(new { id = "P1", name = "P", unit = "EA" }));
        var (provider, logs) = Create(handler, new RetryPolicy(TimeSpan.FromSeconds(1), 3, [TimeSpan.Zero, TimeSpan.Zero]));

        Assert.NotNull(await provider.GetProductAsync("P1", default));

        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal(new[] { false, false, true }, logs.Requests.Select(x => x.IsSuccess));
        Assert.All(logs.Requests, log => { Assert.Equal(IntegrationType.Erp, log.IntegrationType); Assert.Equal("GetProduct", log.Operation); Assert.Equal("api/products/P1", log.ExternalResource); });
    }

    [Fact]
    public async Task TransientFailures_DoNotExceedMaximum()
    {
        var handler = new RecordingHandler(Response(HttpStatusCode.InternalServerError), Response(HttpStatusCode.InternalServerError), Response(HttpStatusCode.InternalServerError));
        var (provider, logs) = Create(handler, new RetryPolicy(TimeSpan.FromSeconds(1), 3, [TimeSpan.Zero, TimeSpan.Zero]));

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.GetProductAsync("P1", default));

        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal(3, logs.Requests.Count);
    }

    [Fact]
    public async Task TransportFailure_IsRetried_AndLaterResponseReturned()
    {
        var handler = new RecordingHandler(new HttpRequestException("secret-token"), Json(new { id = "P1", name = "P", unit = "EA" }));
        var (provider, logs) = Create(handler, new RetryPolicy(TimeSpan.FromSeconds(1), 3, [TimeSpan.Zero, TimeSpan.Zero]));

        Assert.Equal("EA", (await provider.GetProductAsync("P1", default))!.UnitOfMeasure);
        Assert.Equal(2, handler.Requests.Count);
        Assert.DoesNotContain(logs.Requests, x => x.Message?.Contains("secret-token", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task InvalidJsonFailsWithoutRawPayloadOrRetry()
    {
        const string rawSecret = "raw-secret-token";
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(rawSecret) });
        var (provider, logs) = Create(handler);

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => provider.GetProductAsync("P1", default));

        Assert.DoesNotContain(rawSecret, error.Message);
        Assert.Single(handler.Requests);
        Assert.DoesNotContain(logs.Requests, x => x.Message?.Contains(rawSecret, StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task CallerCancellation_ReachesHandler_AndIsNotRetried()
    {
        var handler = new HangingHandler();
        var (provider, logs) = Create(handler, new RetryPolicy(TimeSpan.FromSeconds(10), 3, [TimeSpan.Zero, TimeSpan.Zero]));
        using var source = new CancellationTokenSource();
        source.CancelAfter(TimeSpan.FromMilliseconds(30));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GetProductAsync("P1", source.Token));

        Assert.Equal(1, handler.Count);
        Assert.True(handler.ObservedToken.CanBeCanceled);
        Assert.Single(logs.Requests);
    }

    [Fact]
    public async Task AdapterTimeout_CancelsHangingRequest_AndDoesNotReturnDefault()
    {
        var handler = new HangingHandler();
        var (provider, logs) = Create(handler, new RetryPolicy(TimeSpan.FromMilliseconds(25), 3, [TimeSpan.Zero, TimeSpan.Zero]));

        await Assert.ThrowsAsync<TimeoutException>(() => provider.GetProductAsync("P1", default));

        Assert.Equal(1, handler.Count);
        Assert.False(Assert.Single(logs.Requests).IsSuccess);
    }

    [Fact]
    public async Task CancellationDuringRetryDelay_IsObservedWithoutAnotherAttempt()
    {
        var handler = new RecordingHandler(Response(HttpStatusCode.ServiceUnavailable));
        var (provider, _) = Create(handler, new RetryPolicy(TimeSpan.FromSeconds(1), 3, [TimeSpan.FromSeconds(5), TimeSpan.Zero]));
        using var source = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GetProductAsync("P1", source.Token));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task SameProviderInstance_DoesNotCacheBusinessData()
    {
        var handler = new RecordingHandler(
            Json(new { id = "P1", name = "A", unit = "EA" }),
            Json(new { id = "P1", name = "B", unit = "KG" }));
        var (provider, _) = Create(handler);

        Assert.Equal("EA", (await provider.GetProductAsync("P1", default))!.UnitOfMeasure);
        Assert.Equal("KG", (await provider.GetProductAsync("P1", default))!.UnitOfMeasure);
        Assert.Equal(2, handler.Requests.Count);
    }

    private static (MockErpDataProvider Provider, RecordingLogWriter Logs) Create(HttpMessageHandler handler, RetryPolicy? policy = null)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://mock-erp.test/") };
        var logs = new RecordingLogWriter();
        return (policy is null ? new MockErpDataProvider(client, logs) : new MockErpDataProvider(client, logs, policy), logs);
    }

    private static HttpResponseMessage Json<T>(T value) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8, "application/json")
    };

    private static HttpResponseMessage Response(HttpStatusCode status) => new(status);

    private sealed class RecordingLogWriter : IIntegrationLogWriter
    {
        public List<IntegrationLogRequest> Requests { get; } = [];
        public Task WriteAsync(IntegrationLogRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }
    }

    private sealed record CapturedRequest(HttpMethod Method, Uri Uri, bool HasBody);

    private sealed class RecordingHandler(params object[] outcomes) : HttpMessageHandler
    {
        private readonly Queue<object> _outcomes = new(outcomes);
        public List<CapturedRequest> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new(request.Method, request.RequestUri!, request.Content is not null));
            if (_outcomes.Count == 0)
            {
                throw new InvalidOperationException("No configured HTTP outcome remains.");
            }

            var outcome = _outcomes.Dequeue();
            return outcome is Exception exception
                ? Task.FromException<HttpResponseMessage>(exception)
                : Task.FromResult((HttpResponseMessage)outcome);
        }
    }

    private sealed class HangingHandler : HttpMessageHandler
    {
        public int Count { get; private set; }
        public CancellationToken ObservedToken { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Count++;
            ObservedToken = cancellationToken;
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException();
        }
    }
}
