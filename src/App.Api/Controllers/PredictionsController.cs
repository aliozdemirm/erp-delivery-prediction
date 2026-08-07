using App.Api.Results;
using App.Application.Prediction;
using App.Application.Contracts.Prediction;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class PredictionsController : ControllerBase
{
    private readonly IPredictionCalculationService _predictionService;
    private readonly IWhatIfPredictionCalculationService _whatIfPredictionService;

    public PredictionsController(
        IPredictionCalculationService predictionService,
        IWhatIfPredictionCalculationService whatIfPredictionService)
    {
        _predictionService = predictionService ?? throw new ArgumentNullException(nameof(predictionService));
        _whatIfPredictionService = whatIfPredictionService ?? throw new ArgumentNullException(nameof(whatIfPredictionService));
    }

    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate([FromBody] CalculatePredictionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrderReference))
        {
            return BadRequest("Order reference cannot be empty.");
        }

        var result = await _predictionService.CalculateAsync(request.OrderReference, cancellationToken);

        if (!result.IsSuccess)
        {
            var problemDetails = ResultHttpMapper.ToProblemDetails(result.Error!);
            return new ObjectResult(problemDetails) { StatusCode = problemDetails.Status };
        }

        return Ok(result.Value);
    }

    [HttpPost("simulate")]
    public async Task<IActionResult> Simulate(
        [FromBody] WhatIfPredictionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _whatIfPredictionService.CalculateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            var problemDetails = ResultHttpMapper.ToProblemDetails(result.Error!);
            return new ObjectResult(problemDetails) { StatusCode = problemDetails.Status };
        }

        return Ok(result.Value);
    }
}

public sealed record CalculatePredictionRequest(string OrderReference);
