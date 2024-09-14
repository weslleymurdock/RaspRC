using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Models;
using Shared.Validators;

namespace Shared.Services;
public class TransmitterService : BackgroundService, IBGTxRx
{
    private readonly TimeSpan _period = TimeSpan.FromMilliseconds(20);
    private readonly ILogger<TransmitterService> _logger;
    private readonly NRF24Service<TransmitterService> _nrf;
    private IValidator<Channel> validator = default!;
    public bool IsEnabled { get; set; } = true;
    public string PortName { get; set; } = "/dev/ttyUSB0";
    public Channel Channels { get; set; } = default!;

    public TransmitterService(
        ILogger<TransmitterService> logger,
        IServiceScopeFactory factory)
    {

        _logger = logger;

        using AsyncServiceScope asyncScope = factory.CreateAsyncScope();

        _nrf = asyncScope.ServiceProvider.GetService<NRF24Service<TransmitterService>>()!;

        validator = asyncScope.ServiceProvider.GetRequiredService<ChannelValuesValidator>();

        _nrf.SerialErrorReceived += (sender, e) =>
        {
            _logger.LogError($"Error received: {e}");
        };

        _nrf.SerialDataReceived += (sender, e) =>
        {
            _nrf.DiscardInputBuffer();
        };
        _logger.LogInformation("started receiver service");

    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // ExecuteAsync is executed once and we have to take care of a mechanism ourselves that is kept during operation.
        // To do this, we can use a Periodic Timer, which, unlike other timers, does not block resources.
        // But instead, WaitForNextTickAsync provides a mechanism that blocks a task and can thus be used in a While loop.
        using PeriodicTimer timer = new(_period);

        // When ASP.NET Core is intentionally shut down, the background service receives information
        // via the stopping token that it has been canceled.
        // We check the cancellation to avoid blocking the application shutdown.
        while (!stoppingToken.IsCancellationRequested &&
            await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (!IsEnabled)
            {
                continue;
            }

            try
            {
                Channels = new Channel([1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700]);
                var results = await validator.ValidateAsync(Channels);
                if (results.IsValid)
                {
                    _ = await _nrf.WriteAsync($"{Channels.HexValue}");
                    _logger.LogInformation($"{Channels.HexValue}");
                }

                //await receiverService.(Channels);


            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to execute PeriodicHostedService with exception message {ex.Message}. Good luck next round!");
            }
        }
    }
}


