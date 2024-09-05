using Rx.Services;
using Rx.Models;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register as singleton first so it can be injected through Dependency Injection
builder.Services.AddSingleton<NRFService>();

// Add as hosted service using the instance registered as singleton before
builder.Services.AddHostedService(
    provider => provider.GetRequiredService<NRFService>());

WebApplication app = builder.Build();

app.MapGet("/", () => "RaspRC Receiver");
app.MapGet("/background", (
    NRFService service) =>
{
    return new NRFState(service.IsEnabled);
});
app.MapMethods("/background", new[] { "PATCH" }, (
    NRFState state, 
    NRFService service) =>
{
    service.IsEnabled = state.IsEnabled;
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
