using App.Api.Controllers;
using App.Application.Common;
using App.Application.Prediction;
using App.Application.Contracts.Prediction;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace App.Api.Tests.Controllers;

public class PredictionsControllerTests
{
    private readonly Mock<IPredictionCalculationService> _serviceMock;
    private readonly PredictionsController _controller;
    private readonly Mock<IWhatIfPredictionCalculationService> _whatIfServiceMock;

    public PredictionsControllerTests()
    {
        _serviceMock = new Mock<IPredictionCalculationService>();
        _whatIfServiceMock = new Mock<IWhatIfPredictionCalculationService>();
        _controller = new PredictionsController(_serviceMock.Object, _whatIfServiceMock.Object);
    }

    [Fact]
    public async Task Simulate_PassesExactRequestAndReturnsSuccess()
    {
        var request = new WhatIfPredictionRequest { ProductReference = "P001", Quantity = 10, LocationReference = "istanbul" };
        var predictionResult = new RuleBasedPredictionResult(
            "WHATIF-P001", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            [], [], [], []);
        _whatIfServiceMock.Setup(service => service.CalculateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RuleBasedPredictionResult>.Success(predictionResult));

        var result = await _controller.Simulate(request, CancellationToken.None);

        Assert.Same(predictionResult, Assert.IsType<OkObjectResult>(result).Value);
        _whatIfServiceMock.Verify(service => service.CalculateAsync(request, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Simulate_WhenServiceFails_UsesProblemDetailsMapping()
    {
        var request = new WhatIfPredictionRequest { ProductReference = "P001", Quantity = 10, LocationReference = "istanbul" };
        _whatIfServiceMock.Setup(service => service.CalculateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RuleBasedPredictionResult>.Failure(new Error("Data.Insufficient", "Insufficient.", ErrorType.Validation)));

        var result = Assert.IsType<ObjectResult>(await _controller.Simulate(request, CancellationToken.None));

        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Data.Insufficient", Assert.IsType<ProblemDetails>(result.Value).Extensions["errorCode"]);
    }

    [Fact]
    public async Task Calculate_WithEmptyOrderReference_ReturnsBadRequest()
    {
        var request = new CalculatePredictionRequest("");
        var result = await _controller.Calculate(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Calculate_WhenServiceFails_ReturnsProblemDetails()
    {
        var request = new CalculatePredictionRequest("ORD-1");
        var error = new Error("Test.Error", "Test message", ErrorType.Validation);
        _serviceMock.Setup(s => s.CalculateAsync("ORD-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RuleBasedPredictionResult>.Failure(error));

        var result = await _controller.Calculate(request, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("Test message", problemDetails.Detail);
    }

    [Fact]
    public async Task Calculate_WhenServiceSucceeds_ReturnsOkWithResult()
    {
        var request = new CalculatePredictionRequest("ORD-1");
        var predictionResult = new RuleBasedPredictionResult(
            "ORD-1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            Array.Empty<string>(), Array.Empty<string>(), Array.Empty<MaterialShortage>(), Array.Empty<TimelineItem>());

        _serviceMock.Setup(s => s.CalculateAsync("ORD-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RuleBasedPredictionResult>.Success(predictionResult));

        var result = await _controller.Calculate(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(predictionResult, okResult.Value);
    }
}
