using App.Application.Abstractions.Erp;
using App.Application.Common;
using App.Application.Contracts.Configuration;
using App.Application.Contracts.Erp;
using App.Application.Prediction;
using App.Application.Prediction.Resolvers;
using App.Domain.Abstractions;
using App.Domain.Prediction;
using Moq;
using Xunit;

namespace App.Application.Tests.Prediction;

public class PredictionCalculationServiceTests
{
    private readonly Mock<IErpBatchReader> _erpBatchReaderMock;
    private readonly Mock<ICriticalPathCalculator> _cpmMock;
    private readonly Mock<IClock> _clockMock;
    private readonly PredictionCalculationService _service;
    private readonly DateTimeOffset _now;

    public PredictionCalculationServiceTests()
    {
        _erpBatchReaderMock = new Mock<IErpBatchReader>();
        _cpmMock = new Mock<ICriticalPathCalculator>();
        _clockMock = new Mock<IClock>();
        _now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        _clockMock.Setup(c => c.UtcNow).Returns(_now);

        var options = new MvpAssumptionsOptions
        {
            WorkingCalendar = new WorkingCalendarAssumptionsOptions { MinutesPerDay = 480 },
            Procurement = new ProcurementAssumptionsOptions { FallbackDurationMinutes = 960 },
            Shipping = new ShippingAssumptionsOptions { FallbackDurationMinutes = 1440 }
        };
        var procurementResolver = new ProcurementResolver();
        var capacityResolver = new CapacityResolver();
        var engine = new RuleBasedPredictionEngine(procurementResolver, capacityResolver, _clockMock.Object, options);
        var contextBuilder = new PredictionContextBuilder();
        var shippingResolver = new ShippingResolver();

        _service = new PredictionCalculationService(
            _erpBatchReaderMock.Object,
            contextBuilder,
            engine,
            _cpmMock.Object,
            _clockMock.Object,
            options,
            shippingResolver);
    }

    [Fact]
    public async Task CalculateAsync_WhenErpDataIsInsufficient_ReturnsFailure()
    {
        var snapshot = new ErpBatchSnapshot(
            DateTimeOffset.UtcNow,
            new OrderReadDto("ORD-1", DateTimeOffset.UtcNow, null, null),
            Array.Empty<OrderItemReadDto>(),
            Array.Empty<ProductReadDto>(),
            Array.Empty<BomItemReadDto>(),
            Array.Empty<StockLevelReadDto>(),
            Array.Empty<OpenPurchaseOrderReadDto>(),
            Array.Empty<WorkOrderReadDto>()
        );
        _erpBatchReaderMock.Setup(r => r.ReadAsync("ORD-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ErpBatchSnapshot>.Success(snapshot));

        var result = await _service.CalculateAsync("ORD-1");

        Assert.False(result.IsSuccess);
        Assert.Equal("Data.Insufficient", result.Error!.Code);
    }

    [Fact]
    public async Task CalculateAsync_WhenSuccessful_ReturnsCorrectDatesAndTimeline()
    {
        var snapshot = new ErpBatchSnapshot(
            DateTimeOffset.UtcNow,
            new OrderReadDto("ORD-1", DateTimeOffset.UtcNow, null, null),
            new[] { new OrderItemReadDto("ORD-1", "ITEM-1", "PROD-1", 10, "EA") },
            new[] { new ProductReadDto("PROD-1", "Product 1", "EA"), new ProductReadDto("COMP-1", "Component 1", "EA") },
            new[] { new BomItemReadDto("PROD-1", "COMP-1", 1, "EA", null) },
            new[] { new StockLevelReadDto("COMP-1", "WH1", 10, 0, 10) },
            Array.Empty<OpenPurchaseOrderReadDto>(),
            new[] { new WorkOrderReadDto("WO-1", null, "ORD-1", "PROD-1", new RoutingReadDto("RT-1", new[] { new OperationReadDto("OP-1", 10, "WC-1", 60, Array.Empty<string>()) })) }
        );
        _erpBatchReaderMock.Setup(r => r.ReadAsync("ORD-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ErpBatchSnapshot>.Success(snapshot));

        var cpmSchedule = new[] { new OperationSchedule("OP-1", 0, 60, 0, 60, 0) };
        var cpmResult = new CriticalPathResult(new[] { "OP-1" }, 60, cpmSchedule);
        _cpmMock.Setup(c => c.Calculate(It.IsAny<PredictionContext>()))
            .Returns(CriticalPathOutcome.Success(cpmResult));

        var result = await _service.CalculateAsync("ORD-1");

        Assert.True(result.IsSuccess);
        Assert.Equal("ORD-1", result.Value.OrderReference);
        Assert.Equal(_now, result.Value.EstimatedStart);
        var expectedEnd = _now.AddMinutes(60);
        Assert.Equal(expectedEnd, result.Value.EstimatedEnd);
        Assert.Equal(expectedEnd.AddMinutes(1440), result.Value.EstimatedDelivery);
        Assert.Contains("Shipping duration not found, using fallback", result.Value.AppliedFallbackReasons);
        
        Assert.Single(result.Value.Timeline);
        Assert.Equal("OP-1", result.Value.Timeline[0].OperationRef);
        Assert.True(result.Value.Timeline[0].IsCritical);
        Assert.Equal(_now, result.Value.Timeline[0].EstimatedStart);
        Assert.Equal(_now.AddMinutes(60), result.Value.Timeline[0].EstimatedEnd);
    }
}
