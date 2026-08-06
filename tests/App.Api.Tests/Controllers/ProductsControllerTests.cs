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
    public void ProductResponse_DoesNotExposeProductName()
    {
        var propertyNames = typeof(ProductResponse).GetProperties().Select(p => p.Name).ToArray();

        Assert.DoesNotContain("ProductName", propertyNames);
        Assert.DoesNotContain("Name", propertyNames);
        Assert.Contains("ProductReference", propertyNames);
    }
}
