using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Models; 
using Shared.Validators;
using System.IO.Ports;

namespace Shared.Services;
public class TransmitterService : BackgroundService, IBGTxRx
{
    private readonly TimeSpan _period = TimeSpan.FromMilliseconds(50);
    private readonly ILogger<TransmitterService> _logger; 
    private readonly INRF24Service _nrf;
    private readonly IServiceScopeFactory _factory;
    private IValidator<Channel> validator = default!; 
    private int _executionCount = 0;
    private readonly NRF24 nrf;
    public bool IsEnabled { get; set; } = true;
    public string PortName { get; set; } = "/dev/ttyUSB0";
    public Channel Channels { get; set; } = default!;

    public TransmitterService(
        ILogger<TransmitterService> logger,  
        IOptions<NRF24> options,
        IServiceScopeFactory factory)
    {
        nrf = options.Value;

        _logger = logger;
          
        using AsyncServiceScope asyncScope = factory.CreateAsyncScope();

        _nrf = asyncScope.ServiceProvider.GetService<NRF24Service<TransmitterService>>()!;

        _factory = factory;

        _nrf.SerialErrorReceived += (sender, e) =>
        {
            _logger.LogError($"Error received: {e}");
            Channels.Value = default!;
        };

        _nrf.SerialDataReceived += (sender, e) =>
        {
            SerialPort sp = (SerialPort)sender;
            Channels.HexValue = sp.ReadExisting();
            for (int i = 0; i < Channels.HexValue.Length / 3; i++)
            {
                var value = Channels.HexValue.Substring(i * 3, 3);
                _logger.LogInformation($"{value} : {int.Parse(value, System.Globalization.NumberStyles.HexNumber)}");
            }
            sp.DiscardInBuffer();
        };

    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // ExecuteAsync is executed once and we have to take care of a mechanism ourselves that is kept during operation.
        // To do this, we can use a Periodic Timer, which, unlike other timers, does not block resources.
        // But instead, WaitForNextTickAsync provides a mechanism that blocks a task and can thus be used in a While loop.
        using PeriodicTimer timer = new PeriodicTimer(_period);
        try
        {
            // We cannot use the default dependency injection behavior, because ExecuteAsync is
            // a long-running method while the background service is running.
            // To prevent open resources and instances, only create the services and other references on a run
            // Create scope, so we get request services
            await using AsyncServiceScope asyncScope = _factory.CreateAsyncScope();
            // Get service from scope 
            validator = asyncScope.ServiceProvider.GetRequiredService<ChannelValuesValidator>();
        }
        catch (Exception e)
        {
            _logger.LogError($"{e}");
            throw;
        }

        if (_nrf.NRFPortIsOpen)
        {
            await _nrf.PutConfigurationAsync(nrf);
        }
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
                Channels = new Channel([1000,1000,1000,1000,1000,1000,1000,1000]);
                var results = await validator.ValidateAsync(Channels);
                if (results.IsValid)
                {
                    await _nrf.WriteAsync(Channels.HexValue);
                }

                //await receiverService.(Channels);

                
                // Sample count increment
                _executionCount++;
                _logger.LogInformation($"Executed TransmitterService - Count: {_executionCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to execute PeriodicHostedService with exception message {ex.Message}. Good luck next round!");
            }
        }
    }
}


