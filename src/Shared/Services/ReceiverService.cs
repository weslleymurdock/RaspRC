using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options; 
using Shared.Models; 
namespace Shared.Services;


public class ReceiverService : BackgroundService, IBGTxRx
{
    private readonly TimeSpan _period = TimeSpan.FromMilliseconds(50);
    private readonly INRF24Service _nrf;
    private readonly ILogger<ReceiverService> _logger;
    private readonly IServiceScopeFactory _factory;
    private int _executionCount = 0;
    private readonly NRF24 nrf;
    public bool IsEnabled { get; set; } = true;
    public string PortName { get; set; } = "/dev/ttyUSB0";
    public string Data { get; set; } = string.Empty;

    public ReceiverService(
        ILogger<ReceiverService> logger,
                IOptions<NRF24> options,
        IServiceScopeFactory factory)
    {
        _logger = logger;
        _factory = factory;
        nrf = options.Value;
        using AsyncServiceScope asyncScope = factory.CreateAsyncScope();

        _nrf = asyncScope.ServiceProvider.GetService<NRF24Service<TransmitterService>>()!;

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
        
        await _nrf.PutConfigurationAsync(nrf);
         
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
                _logger.LogInformation($"""Data: {Data}""");
                
                // Sample count increment
                _executionCount++;
                _logger.LogInformation($"Executed ReceiverService - Count: {_executionCount}");
            }
            catch (Exception ex)
            {
                _logger.LogInformation(
                    $"Failed to execute PeriodicHostedService with exception message {ex.Message}. Good luck next round!");
            }
        }
    }
}


