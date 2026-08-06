using Microsoft.AspNetCore.Mvc;
using MockErp.Api.Data;
using MockErp.Api.Models;

namespace MockErp.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(MockErpDataStore dataStore) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MockErpProduct>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<MockErpProduct>> GetAll() =>
        Ok(dataStore.GetProducts());

    [HttpGet("{id}")]
    [ProducesResponseType<MockErpProduct>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<MockErpProduct> GetById(string id)
    {
        var product = dataStore.GetProduct(id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpGet("{id}/bom")]
    [ProducesResponseType<IReadOnlyList<MockErpBomLine>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IReadOnlyList<MockErpBomLine>> GetBom(string id)
    {
        if (dataStore.GetProduct(id) is null)
        {
            return NotFound();
        }

        return Ok(dataStore.GetProductBom(id));
    }
}
