using FluentValidation;
using Rx.Services;
using Shared.Models;
using Shared.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSystemd();
builder.Host.UseSystemd();
// Register as singleton first so it can be injected through Dependency Injection
builder.Services.AddSingleton<ReceiverService>();
//builder.Services.AddSingleton<ReceiverService>();
// Add as hosted service using the instance registered as singleton before
builder.Services.AddHostedService(
    provider => provider.GetRequiredService<ReceiverService>());

WebApplication app = builder.Build();

app.MapGet("/", () => "RaspRC Receiver");
app.MapGet("/receiver", (
    ReceiverService service) =>
{
    return new ReceiverState(service.IsEnabled);
});

app.MapMethods("/receiver", ["PATCH"], (
    ReceiverState state, 
    ReceiverService service) =>
{
    service.IsEnabled = state.IsEnabled;
});

app.MapMethods("/nrf", ["PATCH"], async (
            NRF24 nrf,
            NRF24Service<ReceiverService> service,
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

app.MapGet("/nrf", async (NRF24Service<ReceiverService> service, IValidator<NRF24> validator) =>
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
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.Run();
