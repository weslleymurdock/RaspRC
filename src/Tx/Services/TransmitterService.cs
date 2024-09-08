using FluentValidation;
using Shared.Models;
using Shared.Services;
using System.IO.Ports;

namespace Tx.Services;
public class TransmitterService : BackgroundService
{
    private readonly TimeSpan _period = TimeSpan.FromMilliseconds(50);
    private readonly ILogger<TransmitterService> _logger;
    private readonly IValidator<Channel> _validator;
    private readonly IConfiguration _configuration;
    private readonly INRF24Service _nrf;
    private readonly IServiceScopeFactory _factory;
    private InputService input = default!;
    private int _executionCount = 0;

    public bool IsEnabled { get; set; } = true;
    public string PortName { get; set; } = "/dev/ttyUSB0";
    public Channel Channels { get; set; } = default!;

    public TransmitterService(
        ILogger<TransmitterService> logger,
        INRF24Service iNRF24,
        IValidator<Channel> validator,
        IConfiguration configuration,
        IServiceScopeFactory factory)
    {
        _configuration = configuration;

        _logger = logger;

        _validator = validator;

        _nrf = iNRF24;

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
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await this._nrf.StartAsync();
        await base.StartAsync(cancellationToken);
    }
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await this._nrf.StopAsync();
        await base.StopAsync(cancellationToken);
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
            input = asyncScope.ServiceProvider.GetRequiredService<InputService>();
        }
        catch (Exception e)
        {
            _logger.LogError($"{e}");
            throw;
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
                InType @in = InType.Transmitter ; //default
                _configuration.GetSection("Transmitter:InputType").Bind(@in);
                Channels = input.ReadInputs(@in);
                var results = await _validator.ValidateAsync(Channels);
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
                _logger.LogInformation(
                    $"Failed to execute PeriodicHostedService with exception message {ex.Message}. Good luck next round!");
            }
        }
    }
}


