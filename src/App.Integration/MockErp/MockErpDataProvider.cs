using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using App.Application.Abstractions.Erp;
using App.Application.Contracts.Erp;
using App.Application.IntegrationLogging;
using App.Integration.Models;

namespace App.Integration.MockErp;

internal sealed class MockErpDataProvider : IErpDataProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly RetryPolicy DefaultPolicy = new(TimeSpan.FromSeconds(10), 3, [TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200)]);
    private readonly HttpClient _httpClient;
    private readonly IIntegrationLogWriter _logWriter;
    private readonly RetryPolicy _policy;

    public MockErpDataProvider(HttpClient httpClient, IIntegrationLogWriter logWriter)
        : this(httpClient, logWriter, DefaultPolicy)
    {
    }

    internal MockErpDataProvider(HttpClient httpClient, IIntegrationLogWriter logWriter, RetryPolicy policy)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logWriter = logWriter ?? throw new ArgumentNullException(nameof(logWriter));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public async Task<OrderReadDto?> GetOrderAsync(string orderReference, CancellationToken cancellationToken)
    {
        var order = await GetNullableAsync<MockErpOrder>("GetOrder", $"api/orders/{Path(orderReference)}", cancellationToken);
        return order is null ? null : MapOrder(order);
    }

    public async Task<IReadOnlyList<OrderSummaryReadDto>> GetOrderSummariesAsync(CancellationToken cancellationToken)
    {
        var orders = await GetCollectionAsync<MockErpOrder>("GetOrders", "api/orders", cancellationToken);
        return Array.AsReadOnly(orders.Select(MapOrderSummary).ToArray());
    }

    public async Task<IReadOnlyList<OrderItemReadDto>> GetOrderItemsAsync(string orderReference, CancellationToken cancellationToken)
    {
        var order = await GetNullableAsync<MockErpOrder>("GetOrderItems.Order", $"api/orders/{Path(orderReference)}", cancellationToken);
        if (order is null)
        {
            return Array.Empty<OrderItemReadDto>();
        }

        var product = await GetNullableAsync<MockErpProduct>("GetOrderItems.Product", $"api/products/{Path(order.ProductId)}", cancellationToken);
        if (product is null)
        {
            throw new HttpRequestException("Mock ERP product required by the order was not found.", null, HttpStatusCode.NotFound);
        }

        // The Mock ERP contract represents one order as one deterministic line.
        var lineReference = $"{order.Id}-L1";
        return Array.AsReadOnly([new OrderItemReadDto(order.Id, lineReference, order.ProductId, order.Quantity, product.Unit)]);
    }

    public async Task<ProductReadDto?> GetProductAsync(string productReference, CancellationToken cancellationToken)
    {
        var product = await GetNullableAsync<MockErpProduct>("GetProduct", $"api/products/{Path(productReference)}", cancellationToken);
        return product is null ? null : new ProductReadDto(product.Id, null, product.Unit, product.Name);
    }

    public async Task<IReadOnlyList<ProductReadDto>> GetProductsAsync(CancellationToken cancellationToken)
    {
        var products = await GetCollectionAsync<MockErpProduct>("GetProducts", "api/products", cancellationToken);
        return Array.AsReadOnly(products.Select(product => new ProductReadDto(product.Id, null, product.Unit, product.Name)).ToArray());
    }

    public async Task<IReadOnlyList<BomItemReadDto>> GetProductBomAsync(string productReference, CancellationToken cancellationToken)
    {
        var lines = await GetCollectionAsync<MockErpBomLine>("GetProductBom", $"api/products/{Path(productReference)}/bom", cancellationToken);
        return Array.AsReadOnly(lines.Select(line => new BomItemReadDto(productReference, line.ComponentId, line.Quantity, line.Unit, null)).ToArray());
    }

    public async Task<IReadOnlyList<StockLevelReadDto>> GetStockLevelsAsync(IReadOnlyList<string> productReferences, CancellationToken cancellationToken)
    {
        var values = await GetCollectionAsync<MockErpStockLevel>("GetStockLevels", Query("api/stock-levels", "productReferences", productReferences), cancellationToken);
        return Array.AsReadOnly(values.Select(x => new StockLevelReadDto(x.ProductReference, x.LocationReference, x.OnHandQuantity, x.ReservedQuantity, x.AvailableQuantity)).ToArray());
    }

    public async Task<IReadOnlyList<OpenPurchaseOrderReadDto>> GetOpenPurchaseOrdersAsync(IReadOnlyList<string> productReferences, CancellationToken cancellationToken)
    {
        var values = await GetCollectionAsync<MockErpOpenPurchaseOrder>("GetOpenPurchaseOrders", Query("api/open-purchase-orders", "productReferences", productReferences), cancellationToken);
        return Array.AsReadOnly(values.Select(x => new OpenPurchaseOrderReadDto(x.PurchaseOrderReference, x.ProductReference, x.OpenQuantity, x.ExpectedAvailabilityDateTime, x.SupplierLeadTimeMinutes, x.Status)).ToArray());
    }

    public async Task<IReadOnlyList<WorkOrderReadDto>> GetWorkOrdersAsync(string orderReference, IReadOnlyList<string> productReferences, CancellationToken cancellationToken)
    {
        var query = new List<KeyValuePair<string, string>> { new("orderReference", orderReference) };
        query.AddRange(productReferences.Select(value => new KeyValuePair<string, string>("productReferences", value)));
        var values = await GetCollectionAsync<MockErpWorkOrder>("GetWorkOrders", Query("api/work-orders", query), cancellationToken);
        return Array.AsReadOnly(values.Select(MapWorkOrder).ToArray());
    }

    public async Task<CapacityAndCalendarReadDto> GetCapacityAndCalendarAsync(IReadOnlyList<string> workCenterReferences, DateTimeOffset rangeStart, DateTimeOffset rangeEnd, CancellationToken cancellationToken)
    {
        var query = workCenterReferences.Select(value => new KeyValuePair<string, string>("workCenterReferences", value)).ToList();
        query.Add(new("rangeStart", rangeStart.ToString("O", CultureInfo.InvariantCulture)));
        query.Add(new("rangeEnd", rangeEnd.ToString("O", CultureInfo.InvariantCulture)));
        var value = await GetRequiredAsync<MockErpCapacityAndCalendar>("GetCapacityAndCalendar", Query("api/capacity-calendar", query), cancellationToken);
        return MapCapacity(value);
    }

    public async Task<ShippingDurationReadDto?> GetShippingDurationAsync(string originReference, string destinationReference, string shippingProfileReference, CancellationToken cancellationToken)
    {
        var query = new[]
        {
            new KeyValuePair<string, string>("originReference", originReference),
            new KeyValuePair<string, string>("destinationReference", destinationReference),
            new KeyValuePair<string, string>("shippingProfileReference", shippingProfileReference)
        };
        var value = await GetNullableAsync<MockErpShippingRoute>("GetShippingDuration", Query("api/shipping-durations", query), cancellationToken);
        if (value is null)
        {
            return null;
        }

        var readModel = MapShippingRouteReadModel(value);
        return new ShippingDurationReadDto(
            readModel.OriginReference,
            readModel.DestinationReference,
            readModel.ShippingProfileReference,
            readModel.ShippingDurationMinutes);
    }

    private async Task<T?> GetNullableAsync<T>(string operation, string relativeUri, CancellationToken cancellationToken)
    {
        using var response = await SendWithRetryAsync(operation, relativeUri, cancellationToken, allowNotFound: true);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent)
        {
            return default;
        }

        return await ReadRequiredAsync<T>(response, operation, cancellationToken);
    }

    private async Task<T> GetRequiredAsync<T>(string operation, string relativeUri, CancellationToken cancellationToken)
    {
        using var response = await SendWithRetryAsync(operation, relativeUri, cancellationToken, allowNotFound: false);
        return await ReadRequiredAsync<T>(response, operation, cancellationToken);
    }

    private async Task<IReadOnlyList<T>> GetCollectionAsync<T>(string operation, string relativeUri, CancellationToken cancellationToken)
    {
        using var response = await SendWithRetryAsync(operation, relativeUri, cancellationToken, allowNotFound: false);
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return Array.Empty<T>();
        }

        return await ReadRequiredAsync<T[]>(response, operation, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(string operation, string relativeUri, CancellationToken callerToken, bool allowNotFound)
    {
        for (var attempt = 1; attempt <= _policy.MaximumAttempts; attempt++)
        {
            callerToken.ThrowIfCancellationRequested();
            using var request = new HttpRequestMessage(HttpMethod.Get, relativeUri);
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
            timeoutSource.CancelAfter(_policy.AttemptTimeout);
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutSource.Token);
                stopwatch.Stop();
                var success = response.IsSuccessStatusCode || (allowNotFound && response.StatusCode == HttpStatusCode.NotFound);
                if (success)
                {
                    await WriteLogAsync(operation, relativeUri, true, (int)response.StatusCode, stopwatch.ElapsedMilliseconds,
                        "Mock ERP GET completed.");
                }
                else
                {
                    await WriteFailureWithoutMaskingAsync(operation, relativeUri, (int)response.StatusCode,
                        stopwatch.ElapsedMilliseconds, "Mock ERP GET returned an error status.", new HttpRequestException());
                }

                if (success)
                {
                    return response;
                }

                if (IsTransient(response.StatusCode) && attempt < _policy.MaximumAttempts)
                {
                    response.Dispose();
                    await DelayBeforeRetryAsync(attempt, callerToken);
                    continue;
                }

                var status = response.StatusCode;
                response.Dispose();
                throw new HttpRequestException($"Mock ERP operation '{operation}' failed with HTTP status {(int)status}.", null, status);
            }
            catch (OperationCanceledException) when (!callerToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                var failure = new TimeoutException($"Mock ERP operation '{operation}' timed out after {_policy.AttemptTimeout.TotalMilliseconds:0} ms.");
                await WriteFailureWithoutMaskingAsync(operation, relativeUri, null, stopwatch.ElapsedMilliseconds, "Mock ERP GET timed out.", failure);
                throw failure;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                await WriteFailureWithoutMaskingAsync(operation, relativeUri, null, stopwatch.ElapsedMilliseconds, "Mock ERP GET was cancelled.", new OperationCanceledException());
                throw;
            }
            catch (HttpRequestException exception) when (exception.StatusCode is null)
            {
                stopwatch.Stop();
                await WriteFailureWithoutMaskingAsync(operation, relativeUri, null, stopwatch.ElapsedMilliseconds, "Mock ERP transport failure.", exception);
                if (attempt >= _policy.MaximumAttempts)
                {
                    throw new HttpRequestException($"Mock ERP operation '{operation}' failed due to a transport error.", exception);
                }

                await DelayBeforeRetryAsync(attempt, callerToken);
            }
        }

        throw new InvalidOperationException("The Mock ERP retry policy completed unexpectedly.");
    }

    private async Task DelayBeforeRetryAsync(int completedAttempt, CancellationToken cancellationToken)
    {
        var index = Math.Min(completedAttempt - 1, _policy.RetryDelays.Count - 1);
        if (index >= 0)
        {
            await Task.Delay(_policy.RetryDelays[index], cancellationToken);
        }
    }

    private async Task WriteFailureWithoutMaskingAsync(string operation, string uri, int? statusCode, long duration, string message, Exception original)
    {
        try
        {
            await WriteLogAsync(operation, uri, false, statusCode, duration, message);
        }
        catch when (original is not null)
        {
            // Preserve the outbound ERP failure when audit persistence also fails.
        }
    }

    private Task WriteLogAsync(string operation, string uri, bool success, int? statusCode, long duration, string message)
    {
        var resource = uri.Split('?', 2)[0];
        return _logWriter.WriteAsync(new IntegrationLogRequest(IntegrationType.Erp, operation, resource, success, statusCode, Math.Max(0, duration), message), CancellationToken.None);
    }

    private static async Task<T> ReadRequiredAsync<T>(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        try
        {
            var value = await response.Content.ReadFromJsonAsync<T>(SerializerOptions, cancellationToken);
            return value ?? throw new JsonException("The response JSON was empty.");
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException($"Mock ERP operation '{operation}' returned invalid JSON.", exception, response.StatusCode);
        }
    }

    private static bool IsTransient(HttpStatusCode statusCode) => statusCode is
        HttpStatusCode.RequestTimeout or (HttpStatusCode)429 or HttpStatusCode.InternalServerError or
        HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

    private static string Path(string value) => Uri.EscapeDataString(value);

    private static string Query(string path, string name, IEnumerable<string> values) =>
        Query(path, values.Select(value => new KeyValuePair<string, string>(name, value)));

    private static string Query(string path, IEnumerable<KeyValuePair<string, string>> values) =>
        $"{path}?{string.Join("&", values.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"))}";

    private static OrderReadDto MapOrder(MockErpOrder order) => new(order.Id, ToUtcStartOfDay(order.RequestedDeliveryDate), null, null);

    private static OrderSummaryReadDto MapOrderSummary(MockErpOrder order) =>
        new(order.Id, order.ProductId, order.Quantity, ToUtcStartOfDay(order.RequestedDeliveryDate));

    private static DateTimeOffset ToUtcStartOfDay(DateOnly date) =>
        new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    private static WorkOrderReadDto MapWorkOrder(MockErpWorkOrder value)
    {
        var routing = MapRoutingReadModel(value.Routing);
        return new WorkOrderReadDto(
            value.WorkOrderReference,
            value.OrderReference,
            value.ProductReference,
            value.Status,
            new RoutingReadDto(
                routing.RoutingReference,
                Array.AsReadOnly(routing.Operations.Select(operation => new OperationReadDto(
                    operation.OperationReference,
                    operation.OperationSequence,
                    operation.WorkCenterReference,
                    operation.StandardDurationMinutes,
                    Array.AsReadOnly(operation.PredecessorOperationReferences.ToArray())))
                    .ToArray())));
    }

    internal static RoutingReadModel MapRoutingReadModel(MockErpRouting source) => new(
        source.RoutingReference,
        Array.AsReadOnly(source.Operations.Select(operation => new OperationReadModel(
            operation.OperationReference,
            operation.OperationSequence,
            operation.WorkCenterReference,
            operation.StandardDurationMinutes,
            Array.AsReadOnly(operation.PredecessorOperationReferences.ToArray())))
            .ToArray()));

    internal static ShippingRouteReadModel MapShippingRouteReadModel(MockErpShippingRoute source) =>
        new(
            source.OriginReference,
            source.DestinationReference,
            source.ShippingProfileReference,
            source.ShippingDurationMinutes);

    private static CapacityAndCalendarReadDto MapCapacity(MockErpCapacityAndCalendar value) => new(
        value.RangeStart, value.RangeEnd,
        Array.AsReadOnly(value.WorkCenters.Select(MapWorkCenter).ToArray()),
        Array.AsReadOnly(value.Shifts.Select(x => new WorkingShiftReadDto(x.WorkCenterReference, x.Start, x.End)).ToArray()),
        Array.AsReadOnly(value.Holidays.Select(x => new HolidayReadDto(x.Date, x.WorkCenterReference)).ToArray()),
        Array.AsReadOnly(value.PlannedDowntimes.Select(x => new PlannedDowntimeReadDto(x.WorkCenterReference, x.Start, x.End, x.PlannedDowntimeMinutes)).ToArray()));

    private static WorkCenterReadDto MapWorkCenter(MockErpWorkCenter source)
    {
        var readModel = MapWorkCenterReadModel(source);
        return new WorkCenterReadDto(readModel.WorkCenterRef, readModel.Name);
    }

    // Isolated master-data projection (SAD/T-358): kept as its own step, not inlined into
    // MapWorkCenter, so Integration-side master data can be read/tested independently of
    // the minimal Application-facing Work Center DTO shape.
    internal static WorkCenterReadModel MapWorkCenterReadModel(MockErpWorkCenter source) =>
        new(source.WorkCenterRef, source.Name);
}

internal sealed record RetryPolicy(TimeSpan AttemptTimeout, int MaximumAttempts, IReadOnlyList<TimeSpan> RetryDelays);
