using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Extensions;
using Shared.Models;
using System.IO.Ports;
namespace Shared.Services;


public class ReceiverService : BackgroundService, IBGTxRx
{
    private readonly TimeSpan _period = TimeSpan.FromMilliseconds(20);
    private readonly NRF24Service<ReceiverService> _nrf;
    private readonly ILogger<ReceiverService> _logger;
    public bool IsEnabled { get; set; } = true;
    public string PortName { get; set; } = "/dev/ttyUSB0";
    public string Data { get; set; } = string.Empty;
    public Channel Channels { get; set; } = default!;
    public string NextDataReceived = string.Empty;
    public ReceiverService(
        ILogger<ReceiverService> logger,
        IServiceScopeFactory factory)
    {
        _logger = logger;
        using AsyncServiceScope asyncScope = factory.CreateAsyncScope();

        _nrf = asyncScope.ServiceProvider.GetService<NRF24Service<ReceiverService>>()!;
        _nrf.SerialErrorReceived += (sender, e) =>
        {
            _logger.LogError($"Error received: {e}");
            Channels.Value = default!;
        };

        _nrf.SerialDataReceived += async (sender, e) =>
        {

            try
            {
                SerialPort serial = (SerialPort)sender ;
                Data = await serial.ReadLineAsync();
                while(Data.Length <= 23)
                {
                    Data += await serial.ReadToAsync("\n");
                }
                //logger.LogInformation($"Received data> {Data}");
                serial.DiscardInBuffer();
            }
            catch (Exception ex)
            {
                logger.LogError($"error while receiving data> {ex}");
            }
        };
        _logger.LogInformation("started receiver service");
    }

    /// <summary>
    /// ExecuteAsync method of backgroundservice that reads received data and handle the outputs
    /// </summary>
    /// <remarks>
    /// ExecuteAsync is executed once and we have to take care of a mechanism ourselves that is kept during operation.
    /// To do this, we can use a Periodic Timer, which, unlike other timers, does not block resources.
    /// But instead, WaitForNextTickAsync provides a mechanism that blocks a task and can thus be used in a While loop.
    /// When ASP.NET Core is intentionally shut down, the background service receives information
    /// via the stopping token that it has been canceled.
    /// We check the cancellation to avoid blocking the application shutdown.
    /// </remarks>
    /// <param name="stoppingToken">Cancelation token</param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(_period);

        _logger.LogInformation("started receiver service loop");

        Data = string.Empty;

        _nrf.DiscardInputBuffer();

        _ = await _nrf.ReadLineAsync();

        while (!stoppingToken.IsCancellationRequested &&
            await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (!IsEnabled)
            {
                continue;
            }
            try
            {
                //blocks the loop until the data is ready
                while (Data.Length <= 23) continue;
                _logger.LogInformation($"Data: {Data} {Data.Length} ");
                
                Data = string.Empty;
            }
            catch (Exception ex)
            {
                Data = string.Empty;
                NextDataReceived = string.Empty;
                _logger.LogError(ex, $"Failed to execute PeriodicHostedService with exception . Good luck next round!");
            }
        }
    }
}


