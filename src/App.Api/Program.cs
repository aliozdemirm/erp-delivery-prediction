using App.Api.ExceptionHandling;
using App.Api.Security;
using App.Application.IntegrationLogging;
using App.Infrastructure.Clock;
using App.Infrastructure.Security;
using App.Infrastructure.Shipping;
using App.Integration.MockErp;
using App.Persistence;
using App.Persistence.IntegrationLogging;
using App.Api.Configuration;
using App.Application.Contracts.Configuration;
using App.Application.Erp;
using App.Application.Prediction;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IIntegrationLogWriter, IntegrationLogWriter>();

builder.Services.AddInfrastructureSecurity(builder.Configuration);
builder.Services.AddShippingLookup();
builder.Services.AddSystemClock();
builder.Services.AddMockErpDataProvider(builder.Configuration);
builder.Services.AddErpBatchReader();
builder.Services.AddPredictionServices();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<JwtOptions>((bearerOptions, jwtOptions) =>
    {
        bearerOptions.TokenValidationParameters = JwtBearerOptionsFactory.BuildTokenValidationParameters(jwtOptions);
    });

builder.Configuration.AddMvpAssumptions();
builder.Services.AddMvpAssumptionsOptions(builder.Configuration);

var app = builder.Build();

// Run CategoryAFieldGuard
var options = app.Services.GetRequiredService<IOptions<MvpAssumptionsOptions>>().Value;
var jsonPath = Path.Combine(builder.Environment.ContentRootPath, "mvp-assumptions.json");
var jsonContent = File.ReadAllText(jsonPath);
var logger = app.Services.GetRequiredService<ILogger<Program>>();
CategoryAFieldGuard.Validate(jsonContent, options, logger);

// Global exception handling must run before any other middleware that could throw.
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
