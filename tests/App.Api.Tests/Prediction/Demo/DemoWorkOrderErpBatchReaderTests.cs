using App.Api.Prediction.Demo;
using App.Api.Tests.TestDoubles;
using App.Application.Abstractions.Erp;
using App.Application.Common;
using App.Application.Contracts.Erp;
using App.Application.Prediction.Demo;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace App.Api.Tests.Prediction.Demo;

public class DemoWorkOrderErpBatchReaderTests
{
    private static ErpBatchSnapshot CreateSnapshot(string orderReference, string productReference, IReadOnlyList<WorkOrderReadDto> workOrders)
    {
        var order = new OrderReadDto(orderReference, DateTimeOffset.UtcNow, null, null);
        var orderItems = new List<OrderItemReadDto>
        {
            new(orderReference, "L1", productReference, 5, "EA"),
        };

        return new ErpBatchSnapshot(
            DateTimeOffset.UtcNow,
            order,
            orderItems,
            Array.Empty<ProductReadDto>(),
            Array.Empty<BomItemReadDto>(),
            Array.Empty<StockLevelReadDto>(),
            Array.Empty<OpenPurchaseOrderReadDto>(),
            workOrders);
    }

    [Fact]
    public async Task FlagFalse_ReturnsInnerResultUnchanged()
    {
        var snapshot = CreateSnapshot("SO00001", "P002", Array.Empty<WorkOrderReadDto>());
        var innerMock = new Mock<IErpBatchReader>();
        innerMock.Setup(r => r.ReadAsync("SO00001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ErpBatchSnapshot>.Success(snapshot));

        var options = new DemoWorkOrderOptions { EnableSyntheticWorkOrder = false };
        var logger = new TestLogger<DemoWorkOrderErpBatchReader>();
        var sut = new DemoWorkOrderErpBatchReader(innerMock.Object, options, logger);

        var result = await sut.ReadAsync("SO00001");

        Assert.True(result.IsSuccess);
        Assert.Same(snapshot, result.Value);
        Assert.Empty(result.Value.WorkOrders);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task FlagTrue_WorkOrdersEmpty_InjectsDemoWorkOrderAndLogsWarning()
    {
        var snapshot = CreateSnapshot("SO00001", "P002", Array.Empty<WorkOrderReadDto>());
        var innerMock = new Mock<IErpBatchReader>();
        innerMock.Setup(r => r.ReadAsync("SO00001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ErpBatchSnapshot>.Success(snapshot));

        var options = new DemoWorkOrderOptions { EnableSyntheticWorkOrder = true };
        var logger = new TestLogger<DemoWorkOrderErpBatchReader>();
        var sut = new DemoWorkOrderErpBatchReader(innerMock.Object, options, logger);

        var result = await sut.ReadAsync("SO00001");

        Assert.True(result.IsSuccess);
        var workOrder = Assert.Single(result.Value.WorkOrders);
        Assert.Equal("DEMO-WO-001", workOrder.WorkOrderReference);
        Assert.Equal("SO00001", workOrder.OrderReference);
        Assert.Equal("P002", workOrder.ProductReference);
        Assert.Equal("Released", workOrder.Status);
        Assert.Equal("DEMO-ROUTING-001", workOrder.Routing.RoutingReference);
        Assert.Equal(2, workOrder.Routing.Operations.Count);
        Assert.Equal("DEMO-OP-10", workOrder.Routing.Operations[0].OperationReference);
        Assert.Equal("DEMO-WC-001", workOrder.Routing.Operations[0].WorkCenterReference);
        Assert.Empty(workOrder.Routing.Operations[0].PredecessorOperationReferences);
        Assert.Equal("DEMO-OP-20", workOrder.Routing.Operations[1].OperationReference);
        Assert.Equal(["DEMO-OP-10"], workOrder.Routing.Operations[1].PredecessorOperationReferences);

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("DEMO-WO-001"));
    }

    [Fact]
    public async Task FlagTrue_RealWorkOrdersPresent_DoesNotOverride()
    {
        var realWorkOrder = new WorkOrderReadDto(
            "WO-REAL-1", "SO00001", "P002", "Released",
            new RoutingReadDto("R-REAL", new List<OperationReadDto>
            {
                new("OP-REAL-10", 10, "WC-REAL", 30, Array.Empty<string>()),
            }));
        var snapshot = CreateSnapshot("SO00001", "P002", new List<WorkOrderReadDto> { realWorkOrder });

        var innerMock = new Mock<IErpBatchReader>();
        innerMock.Setup(r => r.ReadAsync("SO00001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ErpBatchSnapshot>.Success(snapshot));

        var options = new DemoWorkOrderOptions { EnableSyntheticWorkOrder = true };
        var logger = new TestLogger<DemoWorkOrderErpBatchReader>();
        var sut = new DemoWorkOrderErpBatchReader(innerMock.Object, options, logger);

        var result = await sut.ReadAsync("SO00001");

        Assert.True(result.IsSuccess);
        Assert.Same(snapshot, result.Value);
        var workOrder = Assert.Single(result.Value.WorkOrders);
        Assert.Equal("WO-REAL-1", workOrder.WorkOrderReference);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task FlagTrue_InnerFailure_ReturnsFailureUnchanged()
    {
        var error = new Error("ErpBatchReader.OrderNotFound", "Order SO-MISSING could not be found.", ErrorType.NotFound);
        var innerMock = new Mock<IErpBatchReader>();
        innerMock.Setup(r => r.ReadAsync("SO-MISSING", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ErpBatchSnapshot>.Failure(error));

        var options = new DemoWorkOrderOptions { EnableSyntheticWorkOrder = true };
        var logger = new TestLogger<DemoWorkOrderErpBatchReader>();
        var sut = new DemoWorkOrderErpBatchReader(innerMock.Object, options, logger);

        var result = await sut.ReadAsync("SO-MISSING");

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
        Assert.Empty(logger.Entries);
    }
}
