using App.Api.Controllers;
using App.Application.Abstractions.Erp;
using App.Application.Contracts.Erp;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace App.Api.Tests.Controllers;

public class OrdersControllerTests
{
    private readonly Mock<IErpDataProvider> _erpDataProviderMock;
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        _erpDataProviderMock = new Mock<IErpDataProvider>();
        _controller = new OrdersController(_erpDataProviderMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithMappedSummaries()
    {
        var deliveryDate = new DateTimeOffset(2026, 8, 6, 0, 0, 0, TimeSpan.Zero);
        _erpDataProviderMock.Setup(p => p.GetOrderSummariesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrderSummaryReadDto>
            {
                new("SO00001", "P002", 16m, deliveryDate),
            });

        var result = await _controller.GetAll(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var summaries = Assert.IsAssignableFrom<IReadOnlyList<OrderSummaryResponse>>(okResult.Value);
        var summary = Assert.Single(summaries);
        Assert.Equal("SO00001", summary.OrderReference);
        Assert.Equal("P002", summary.ProductReference);
        Assert.Equal(16m, summary.Quantity);
        Assert.Equal(deliveryDate, summary.RequestedDeliveryDateTime);
    }

    [Fact]
    public async Task GetAll_ReturnsResultsSortedByOrderReference()
    {
        var date = DateTimeOffset.UtcNow;
        _erpDataProviderMock.Setup(p => p.GetOrderSummariesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrderSummaryReadDto>
            {
                new("SO00003", "P001", 1m, date),
                new("SO00001", "P002", 2m, date),
                new("SO00002", "P003", 3m, date),
            });

        var result = await _controller.GetAll(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var summaries = Assert.IsAssignableFrom<IReadOnlyList<OrderSummaryResponse>>(okResult.Value);
        Assert.Equal(["SO00001", "SO00002", "SO00003"], summaries.Select(s => s.OrderReference));
    }

    [Fact]
    public async Task GetAll_WithNoOrders_ReturnsOkWithEmptyList()
    {
        _erpDataProviderMock.Setup(p => p.GetOrderSummariesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<OrderSummaryReadDto>());

        var result = await _controller.GetAll(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var summaries = Assert.IsAssignableFrom<IReadOnlyList<OrderSummaryResponse>>(okResult.Value);
        Assert.Empty(summaries);
    }
}
