using App.Application.Contracts.Configuration;

namespace App.Api.Configuration;

public static class MvpAssumptionsConfigurationExtensions
{
    public static IConfigurationBuilder AddMvpAssumptions(this IConfigurationBuilder configuration)
    {
        return configuration.AddJsonFile(
            "mvp-assumptions.json",
            optional: false,
            reloadOnChange: true);
    }

    public static IServiceCollection AddMvpAssumptionsOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<MvpAssumptionsOptions>()
            .Bind(configuration.GetSection(MvpAssumptionsOptions.SectionName))
            .Validate(
                options => options.WorkingCalendar.MinutesPerDay > 0,
                "Working calendar minutes per day must be greater than zero.")
            .Validate(
                options => options.Procurement.FallbackDurationMinutes > 0,
                "Procurement fallback duration must be greater than zero.")
            .Validate(
                options => options.Shipping.FallbackDurationMinutes is null or > 0,
                "Shipping fallback duration must be null or greater than zero.")
            .ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MvpAssumptionsOptions>>().Value);

        return services;
    }
}
