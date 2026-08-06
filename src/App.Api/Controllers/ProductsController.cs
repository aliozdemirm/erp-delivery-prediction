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
                product.PlanningClassification,
                product.UnitOfMeasure))
            .ToList();

        return Ok(response);
    }
}

public sealed record ProductResponse(
    string ProductReference,
    string? PlanningClassification,
    string UnitOfMeasure);
