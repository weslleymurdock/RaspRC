using FluentValidation;
using Shared.Models;
using Shared.Services;
using Shared.Validators;
using System.IO.Ports;
using Tx.Services; 

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSystemd();
builder.Host.UseSystemd();
// Register as singleton first so it can be injected through Dependency Injection 
builder.Services.AddSingleton<TransmitterService>();
builder.Services.AddSingleton<INRF24Service, NRF24Service<TransmitterService>>();
builder.Services.AddScoped<IValidator<Channel>, ChannelValuesValidator>();

// Add as hosted service using the instance registered as singleton before
builder.Services.AddHostedService(
    provider => provider.GetRequiredService<TransmitterService>());


WebApplication app = builder.Build();



app.MapGet("/", () => new { Device = "RaspRC Transmitter" });
app.MapGet("/transmitter", (
    TransmitterService service) =>
{
    return Results.Ok(new { Enabled = new TransmitterState(service.IsEnabled) });
})
.WithName("GetTransmitter")
.WithOpenApi();

app.MapMethods("/transmitter", ["PATCH"], (
    TransmitterState state,
    TransmitterService service) =>
{
    service.IsEnabled = state.IsEnabled;
})
.WithName("PatchTransmitter")
.WithOpenApi();

app.MapGet("/serialports", () =>
{
    return Results.Ok(new { Ports = SerialPort.GetPortNames() });
})
.WithName("GetSerialPorts")
.WithOpenApi();

app.MapMethods("/nrf", ["PATCH"], async (
            NRF24 nrf,
            NRF24Service<TransmitterService> service,
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

app.MapGet("/nrf", async (NRF24Service<TransmitterService> service, IValidator<NRF24> validator) =>
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

// PutConfigurationAsync the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();

