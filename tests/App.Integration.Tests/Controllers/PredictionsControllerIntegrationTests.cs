using System.Net;
using System.Net.Http.Json;
using App.Api.Controllers;
using App.Application.Common;
using App.Application.Prediction;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace App.Integration.Tests.Controllers;

public class PredictionsControllerIntegrationTests : IClassFixture<WebApplicationFactory<App.Api.Controllers.PredictionsController>>
{
    private readonly WebApplicationFactory<App.Api.Controllers.PredictionsController> _factory;

    public PredictionsControllerIntegrationTests(WebApplicationFactory<App.Api.Controllers.PredictionsController> factory)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Host=localhost;Database=dummy;Username=dummy;Password=dummy");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "dummy");
        Environment.SetEnvironmentVariable("Jwt__Audience", "dummy");
        Environment.SetEnvironmentVariable("Jwt__Secret", "super_secret_dummy_key_for_testing_purposes");
        Environment.SetEnvironmentVariable("MockErp__BaseAddress", "http://localhost:5288");
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var mockService = new Mock<IPredictionCalculationService>();
                mockService.Setup(s => s.CalculateAsync("SO-1001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<RuleBasedPredictionResult>.Success(new RuleBasedPredictionResult(
                        "SO-1001",
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow.AddDays(1),
                        DateTimeOffset.UtcNow.AddDays(2),
                        Array.Empty<string>(),
                        Array.Empty<string>(),
                        Array.Empty<App.Application.Prediction.MaterialShortage>(),
                        Array.Empty<App.Application.Prediction.TimelineItem>()
                    )));
                
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPredictionCalculationService));
                if (descriptor != null) services.Remove(descriptor);
                
                services.AddTransient<IPredictionCalculationService>(_ => mockService.Object);
            });
        });
    }

    [Fact]
    public async Task Calculate_WithValidOrderReference_ReturnsExpectedStatus()
    {
        var client = _factory.CreateClient();
        var request = new CalculatePredictionRequest("SO-1001");

        var response = await client.PostAsJsonAsync("/predictions/calculate", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    
    [Fact]
    public async Task Calculate_WithEmptyOrderReference_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var request = new CalculatePredictionRequest(""); 

        var response = await client.PostAsJsonAsync("/predictions/calculate", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
