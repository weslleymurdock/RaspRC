using Shared.Models;
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
//builder.Services.AddSingleton<ReceiverService>();
// Add as hosted service using the instance registered as singleton before
builder.Services.AddHostedService(
    provider => provider.GetRequiredService<TransmitterService>());

WebApplication app = builder.Build();

app.MapGet("/", () => "RaspRC Transmitter");
app.MapGet("/transmitter", (
    TransmitterService service) =>
{
    return new TransmitterState(service.IsEnabled);
})
.WithName("GetTransmitter")
.WithOpenApi();

app.MapMethods("/transmitter", ["PATCH"], (
    TransmitterState state,
    TransmitterService service) =>
{
    service.IsEnabled = state.IsEnabled;
})
.WithName("PatchReceiver")
.WithOpenApi();

app.MapGet("/serialports", () =>
{
    var ports =  SerialPort.GetPortNames()
        .ToArray();
    return ports;
})
.WithName("GetSerialPorts")
.WithOpenApi();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
 
