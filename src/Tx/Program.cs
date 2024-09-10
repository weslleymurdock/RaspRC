using FluentValidation; 
using Tx.Services;
using Shared.Models;
using Shared.Services;
using Shared.Validators;
using System.IO.Ports;

var builder = WebApplication.CreateBuilder(args); 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSystemd();
builder.Host.UseSystemd();
builder.Services.AddValidatorsFromAssemblyContaining<NRFValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<ChannelValuesValidator>();
builder.Services.AddScoped<NRF24Service<TransmitterService>>();
// Register as singleton first so it can be injected through Dependency Injection 
builder.Services.AddSingleton<TransmitterService>();
builder.Services.AddScoped<InputService>();
// Add as hosted service using the instance registered as singleton before
builder.Services.AddHostedService(
    provider => provider.GetRequiredService<TransmitterService>());

builder.Services.Configure<NRF24>(builder.Configuration.GetSection("NRF24"));

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

app.MapGet("/nrf", async (NRF24Service<TransmitterService> service, IValidator<NRF24> validator) =>
{
    var config = await service.GetConfigurationAsync();

    var results = await validator!.ValidateAsync(config);

    if (results.IsValid)
    {
        return Results.Ok(config);
    }

    return Results.ValidationProblem(results.ToDictionary());
})
.WithName("GetNRF")
.WithOpenApi();
app.MapMethods("/nrf", ["PUT"], async (NRF24 nrf, NRF24Service<TransmitterService> service, IValidator<NRF24> validator) =>
{
    var results = await validator!.ValidateAsync(nrf);

    if (!results.IsValid)
    {
        return Results.ValidationProblem(results.ToDictionary());
    }
    var ok = await service.PutConfigurationAsync(nrf);
    return Results.Ok(ok);
})
.WithName("PutNRF")
.WithOpenApi();


// PutConfigurationAsync the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.Run();

