using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Shared.Models;
using Shared.Services;

namespace Shared.Endpoints.NRF;

public static class NRFEndpoint
{
    private static readonly string _route = "/api/hardware/nrf";
    public static WebApplication MapNrf<T>(this WebApplication builder) where T : BackgroundService
    {
        return builder
        .MapPatchConfiguration<T>()
        .MapGetConfiguration<T>();
    }

    private static WebApplication MapGetConfiguration<T>(this WebApplication builder) where T : BackgroundService
    {
        builder.MapGet(_route, async (NRF24Service<T> service, IValidator<NRF24> validator) =>
        {
            _ = service.StopAsync();

            var config = await service.GetConfigurationAsync();

            var results = await validator.ValidateAsync(config);

            _ = service.StartAsync();

            if (results.IsValid)
            {
                return Results.Ok(config);
            }

            return Results.ValidationProblem(results.ToDictionary());
        })
       .WithName("GetNRF")
       .WithOpenApi();
        return builder;
    }

    private static WebApplication MapPatchConfiguration<T>(this WebApplication builder) where T : BackgroundService
    {
        builder.MapMethods(_route, ["PATCH"], async (
            NRF24 nrf,
            NRF24Service<T> service,
            IValidator<NRF24> validator) =>
        {
            var results = await validator.ValidateAsync(nrf);
            if (!results.IsValid)
            {
                return Results.ValidationProblem(results.ToDictionary());
            }
            return Results.NoContent();
        })
        .WithName("PatchNRF")
        .WithOpenApi();
        return builder;
    }
}
