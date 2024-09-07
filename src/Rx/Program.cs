using Rx.Services;
using Shared.Models;
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
