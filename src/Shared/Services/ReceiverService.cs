using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options; 
using Shared.Models;
using System.IO.Ports;
namespace Shared.Services;


public class ReceiverService : BackgroundService, IBGTxRx
{
    private readonly TimeSpan _period = TimeSpan.FromMilliseconds(50);
    private readonly INRF24Service _nrf;
    private readonly ILogger<ReceiverService> _logger; 
    private int _executionCount = 0; 
    public bool IsEnabled { get; set; } = true;
    public string PortName { get; set; } = "/dev/ttyUSB0";
    public string Data { get; set; } = string.Empty;
    public Channel Channels { get; set; } = default!;

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

        _nrf.SerialDataReceived += (sender, e) =>
        {
            try
            {
                SerialPort sp = (SerialPort)sender;
                Channels.HexValue = sp.ReadExisting();
                for (int i = 0; i < Channels.HexValue.Length / 3; i++)
                {
                    var value = Channels.HexValue.Substring(i * 3, 3);
                    _logger.LogInformation($"{value} : {int.Parse(value, System.Globalization.NumberStyles.HexNumber)}");
                }
                sp.DiscardInBuffer();
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
        _nrf.DiscartInputBuffer(); //remove data on first run to sync 
        while (!stoppingToken.IsCancellationRequested &&
            await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (!IsEnabled)
            {
                continue;
            } 
            try
            { 
                 
                Data = await _nrf.ReadAsync();
                _logger.LogInformation($"Data: {Data} Length: {Data.Length}");
                
                // Sample count increment
                _executionCount++;
                _logger.LogInformation($"Executed ReceiverService - Count: {_executionCount}");
            }
            catch (OperationCanceledException e)
            {
                _logger.LogError($"e carai {e}");
            }
            catch (Exception ex)
            {
                _logger.LogInformation(
                    $"Failed to execute PeriodicHostedService with exception message {ex.Message}. Good luck next round!");
            }
        }
    }
}


