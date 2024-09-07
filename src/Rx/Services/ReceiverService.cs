using System.IO.Ports; 
using System.Text;

namespace Rx.Services;


public class ReceiverService : BackgroundService
{
    private readonly TimeSpan _period = TimeSpan.FromMilliseconds(50);
    private readonly ILogger<ReceiverService> _logger;
    private readonly IServiceScopeFactory _factory;
    private int _executionCount = 0;
    
    public bool IsEnabled { get; set; } = true;
    public string PortName { get; set; } = "/dev/ttyUSB0";
    public string Data { get; set; } = string.Empty;
    
    public ReceiverService(
        ILogger<ReceiverService> logger, 
        IServiceScopeFactory factory)
    {
        _logger = logger;
        _factory = factory;
    }

    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // ExecuteAsync is executed once and we have to take care of a mechanism ourselves that is kept during operation.
        // To do this, we can use a Periodic Timer, which, unlike other timers, does not block resources.
        // But instead, WaitForNextTickAsync provides a mechanism that blocks a task and can thus be used in a While loop.
        using PeriodicTimer timer = new PeriodicTimer(_period);
        using SerialPort serial = new SerialPort(PortName);
        serial.BaudRate = 9600;
        serial.Parity = Parity.None;
        serial.DataBits = 8;
        serial.StopBits = StopBits.One;
        serial.DataReceived += (sender, e) => {
            SerialPort sp = (SerialPort)sender;
            Data = sp.ReadExisting();
            for (int i = 0; i < Data.Length; i++)
            {
                _logger.LogInformation($"{(byte)Data[i]} : {(char)Data[i]}");
            }
            sp.DiscardInBuffer();
        };

        try 
        {
            serial.Open();
        }
        catch (Exception e)
        {
            _logger.LogError($"Error to open serial port: {e}");
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
            
            if (!serial.IsOpen) 
            {
                try 
                {
                    serial.Open();
                }
                catch (Exception e)
                {
                    _logger.LogError($"Error to open SerialPort {e}");
                    continue;
                }
            }
            
            try
            {
                // We cannot use the default dependency injection behavior, because ExecuteAsync is
                // a long-running method while the background service is running.
                // To prevent open resources and instances, only create the services and other references on a run
                // Create scope, so we get request services
                await using AsyncServiceScope asyncScope = _factory.CreateAsyncScope();
                    
                // Get service from scope
                ReceiverService receiverService = asyncScope.ServiceProvider.GetRequiredService<ReceiverService>();                
        
                if (string.IsNullOrEmpty(Data) || string.IsNullOrWhiteSpace(Data))
                {
                    Data = serial.ReadExisting();
                }

                //await receiverService.(Data);
                 
                Data = string.Empty;
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


