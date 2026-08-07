using App.Application.Abstractions.Erp;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class ProductsController : ControllerBase
{
    private readonly IErpDataProvider _erpDataProvider;

    public ProductsController(IErpDataProvider erpDataProvider)
    {
        _erpDataProvider = erpDataProvider ?? throw new ArgumentNullException(nameof(erpDataProvider));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var products = await _erpDataProvider.GetProductsAsync(cancellationToken);

        var response = products
            .OrderBy(product => product.ProductReference, StringComparer.Ordinal)
            .Select(product => new ProductResponse(
                product.ProductReference,
                product.Name,
                product.PlanningClassification,
                product.UnitOfMeasure))
            .ToList();

        return Ok(response);
    }

    [HttpGet("stock")]
    public async Task<ActionResult<IReadOnlyList<ProductStockResponse>>> GetStock(CancellationToken cancellationToken)
    {
        var products = await _erpDataProvider.GetProductsAsync(cancellationToken);
        var productReferences = products.Select(product => product.ProductReference).ToList();
        var stockLevels = await _erpDataProvider.GetStockLevelsAsync(productReferences, cancellationToken);

        var availableByProduct = stockLevels
            .GroupBy(stock => stock.ProductReference, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(stock => stock.AvailableQuantity), StringComparer.Ordinal);

        var response = products
            .OrderBy(product => product.ProductReference, StringComparer.Ordinal)
            .Select(product => new ProductStockResponse(
                product.ProductReference,
                product.Name,
                product.UnitOfMeasure,
                availableByProduct.TryGetValue(product.ProductReference, out var available) ? available : 0m))
            .ToList();

        return Ok(response);
    }
}

public sealed record ProductResponse(
    string ProductReference,
    string? Name,
    string? PlanningClassification,
    string UnitOfMeasure);

public sealed record ProductStockResponse(
    string ProductReference,
    string? Name,
    string UnitOfMeasure,
    decimal AvailableQuantity);
