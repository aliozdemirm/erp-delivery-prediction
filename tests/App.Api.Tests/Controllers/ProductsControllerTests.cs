using App.Api.Controllers;
using App.Application.Abstractions.Erp;
using App.Application.Contracts.Erp;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace App.Api.Tests.Controllers;

public class ProductsControllerTests
{
    private readonly Mock<IErpDataProvider> _erpDataProviderMock;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _erpDataProviderMock = new Mock<IErpDataProvider>();
        _controller = new ProductsController(_erpDataProviderMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithMappedProducts()
    {
        _erpDataProviderMock.Setup(p => p.GetProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReadDto>
            {
                new("P002", null, "Adet"),
            });

        var result = await _controller.GetAll(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var products = Assert.IsAssignableFrom<IReadOnlyList<ProductResponse>>(okResult.Value);
        var product = Assert.Single(products);
        Assert.Equal("P002", product.ProductReference);
        Assert.Null(product.PlanningClassification);
        Assert.Equal("Adet", product.UnitOfMeasure);
    }

    [Fact]
    public async Task GetAll_ReturnsResultsSortedByProductReference()
    {
        _erpDataProviderMock.Setup(p => p.GetProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReadDto>
            {
                new("P003", null, "Adet"),
                new("P001", null, "Adet"),
                new("P002", null, "Adet"),
            });

        var result = await _controller.GetAll(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var products = Assert.IsAssignableFrom<IReadOnlyList<ProductResponse>>(okResult.Value);
        Assert.Equal(["P001", "P002", "P003"], products.Select(p => p.ProductReference));
    }

    [Fact]
    public async Task GetAll_WithNoProducts_ReturnsOkWithEmptyList()
    {
        _erpDataProviderMock.Setup(p => p.GetProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ProductReadDto>());

        var result = await _controller.GetAll(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var products = Assert.IsAssignableFrom<IReadOnlyList<ProductResponse>>(okResult.Value);
        Assert.Empty(products);
    }

    [Fact]
    public async Task GetAll_MapsProductNameFromErp()
    {
        _erpDataProviderMock.Setup(p => p.GetProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReadDto>
            {
                new("P001", null, "Adet", "Masa"),
            });

        var result = await _controller.GetAll(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var products = Assert.IsAssignableFrom<IReadOnlyList<ProductResponse>>(okResult.Value);
        Assert.Equal("Masa", Assert.Single(products).Name);
    }

    [Fact]
    public async Task GetAll_LeavesNameNull_WhenErpDoesNotProvideOne()
    {
        _erpDataProviderMock.Setup(p => p.GetProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReadDto> { new("P001", null, "Adet") });

        var result = await _controller.GetAll(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var products = Assert.IsAssignableFrom<IReadOnlyList<ProductResponse>>(okResult.Value);
        Assert.Null(Assert.Single(products).Name);
    }

    [Fact]
    public async Task GetStock_ReturnsAvailableQuantityPerProduct()
    {
        _erpDataProviderMock.Setup(p => p.GetProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReadDto>
            {
                new("P001", null, "Adet", "Masa"),
                new("P002", null, "Adet", "Sandalye"),
            });
        _erpDataProviderMock.Setup(p => p.GetStockLevelsAsync(
                It.Is<IReadOnlyList<string>>(refs => refs.SequenceEqual(new[] { "P001", "P002" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockLevelReadDto>
            {
                new("P001", null, 309m, 0m, 309m),
                new("P002", null, 500m, 0m, 500m),
            });

        var result = await _controller.GetStock(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var stock = Assert.IsAssignableFrom<IReadOnlyList<ProductStockResponse>>(okResult.Value);
        Assert.Equal(2, stock.Count);
        Assert.Equal("P001", stock[0].ProductReference);
        Assert.Equal("Masa", stock[0].Name);
        Assert.Equal(309m, stock[0].AvailableQuantity);
        Assert.Equal("Adet", stock[0].UnitOfMeasure);
        Assert.Equal("P002", stock[1].ProductReference);
        Assert.Equal("Sandalye", stock[1].Name);
        Assert.Equal(500m, stock[1].AvailableQuantity);
    }

    [Fact]
    public async Task GetStock_ReturnsResultsSortedByProductReference()
    {
        _erpDataProviderMock.Setup(p => p.GetProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReadDto>
            {
                new("P003", null, "Adet"),
                new("P001", null, "Adet"),
            });
        _erpDataProviderMock.Setup(p => p.GetStockLevelsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StockLevelReadDto>());

        var result = await _controller.GetStock(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var stock = Assert.IsAssignableFrom<IReadOnlyList<ProductStockResponse>>(okResult.Value);
        Assert.Equal(["P001", "P003"], stock.Select(s => s.ProductReference));
    }

    [Fact]
    public async Task GetStock_DefaultsToZero_WhenNoStockRecordExistsForProduct()
    {
        _erpDataProviderMock.Setup(p => p.GetProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReadDto> { new("P004", null, "Adet") });
        _erpDataProviderMock.Setup(p => p.GetStockLevelsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StockLevelReadDto>());

        var result = await _controller.GetStock(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var stock = Assert.IsAssignableFrom<IReadOnlyList<ProductStockResponse>>(okResult.Value);
        var product = Assert.Single(stock);
        Assert.Equal(0m, product.AvailableQuantity);
    }

    [Fact]
    public async Task GetStock_SumsAvailableQuantityAcrossMultipleLocations()
    {
        _erpDataProviderMock.Setup(p => p.GetProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReadDto> { new("P001", null, "Adet") });
        _erpDataProviderMock.Setup(p => p.GetStockLevelsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockLevelReadDto>
            {
                new("P001", "istanbul", 100m, 0m, 100m),
                new("P001", "ankara", 50m, 0m, 50m),
            });

        var result = await _controller.GetStock(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var stock = Assert.IsAssignableFrom<IReadOnlyList<ProductStockResponse>>(okResult.Value);
        Assert.Equal(150m, Assert.Single(stock).AvailableQuantity);
    }
}
