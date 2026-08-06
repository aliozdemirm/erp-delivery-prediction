using App.Application.Abstractions.Erp;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class OrdersController : ControllerBase
{
    private readonly IErpDataProvider _erpDataProvider;

    public OrdersController(IErpDataProvider erpDataProvider)
    {
        _erpDataProvider = erpDataProvider ?? throw new ArgumentNullException(nameof(erpDataProvider));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderSummaryResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var summaries = await _erpDataProvider.GetOrderSummariesAsync(cancellationToken);

        var response = summaries
            .OrderBy(summary => summary.OrderReference, StringComparer.Ordinal)
            .Select(summary => new OrderSummaryResponse(
                summary.OrderReference,
                summary.ProductReference,
                summary.Quantity,
                summary.RequestedDeliveryDateTime))
            .ToList();

        return Ok(response);
    }
}

public sealed record OrderSummaryResponse(
    string OrderReference,
    string ProductReference,
    decimal Quantity,
    DateTimeOffset RequestedDeliveryDateTime);
