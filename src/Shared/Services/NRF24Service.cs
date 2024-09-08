using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Models;
using Shared.Extensions;
using System.Buffers.Binary;
using System.IO.Ports;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FluentValidation;
using System.Text;
using Microsoft.AspNetCore.Builder;

namespace Shared.Services;

public class NRF24Service<T> : IDisposable, INRF24Service
    where T : BackgroundService
{
    private readonly IValidator<Channel> _channelValidator;
    
    private readonly IValidator<NRF24> _nrfValidator;

    private readonly List<int> _bauds = [4800, 9600, 14400, 19200, 38400, 115200];

    private readonly List<int> _crc = [8, 16];

    private readonly List<int> _rates = [250, 1, 2];

    private readonly IServiceProvider _serviceProvider;

    private readonly ILogger<NRF24Service<T>> _logger;

    private readonly IConfiguration _configuration;

    private static SerialPort serial = new();

    public NRF24 NRF24 { get; set; } = default!;

    public string[] Ports => SerialPort.GetPortNames();

    public event SerialDataReceivedEventHandler SerialDataReceived = default!;

    public event SerialErrorReceivedEventHandler SerialErrorReceived = default!;

    public NRF24Service(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<NRF24Service<T>> logger, IValidator<Channel> channelValidator, IValidator<NRF24> nrfValidator, string section)
    {
        _channelValidator = channelValidator;

        _nrfValidator = nrfValidator;

        _configuration = configuration;

        _logger = logger;

        _serviceProvider = serviceProvider;

        _configuration.GetSection($"{section}:NRF24").Bind(NRF24);

        if (NRF24 is null)
        {
            throw new ArgumentNullException(nameof(NRF24));
        }

        var results = _nrfValidator.ValidateAsync(NRF24).GetAwaiter().GetResult();

        if (!results.IsValid)
        {
            _logger.LogCritical("Invalid radio Configuration");
        
            throw new InvalidDataException("Invalid radio configuration");
        }

        if (!Ports.Contains(NRF24.PortName))
        {
            if (Ports.Length > 1)
            {
                NRF24.PortName = Ports[1];
            }
        }

        serial.PortName = NRF24.PortName;
        serial.BaudRate = _bauds[NRF24.BaudRate - 1];
        serial.DataBits = 8;
        serial.Parity = Parity.None;
        serial.StopBits = StopBits.One;
        serial.DataReceived += SerialDataReceived;
        serial.ErrorReceived += SerialErrorReceived;

        try
        {
            _logger.LogInformation($"opening serial port {NRF24.PortName}");
            serial.Open();
        }
        catch (Exception e)
        {
            _logger.LogError($"Error at opening serial port: \n {e}");
            throw;
        }

        if (serial.IsOpen)
        {
            _ = this.PutConfigurationAsync(NRF24);
        }
    }

    public async Task<NRF24> GetConfigurationAsync()
    {
        var worker = _serviceProvider.GetServices<IHostedService>()
                                    .OfType<T>()
                                    .FirstOrDefault();
        
        await worker!.StopAsync(new CancellationToken(true));
        
        serial.WriteLine($"AT?");
        
        await Task.Delay(2);
        
        var response = serial.ReadExisting().ConfigurationResponse();
        
        ArgumentNullException.ThrowIfNull(response,nameof(response));

        ArgumentOutOfRangeException.ThrowIfLessThan(response.Length, 8, nameof(response.Length));

        response[3] = response[3].StartsWith("00") ? response[3].Remove(0, 1) : response[3];

        _ = int.TryParse(response[4].Replace(".", "").Replace("GHz", ""), out int i);

        _ = int.TryParse(response[5].Replace("CRC", ""), out int j);
         
        response[7] = response[7].Contains("Kbps") ? response[7].Replace("Kbps", "") : response[7].Contains("Mbps") ? response[7].Replace("Mbps", "") : response[7];
         
        NRF24 nrf = new NRF24()
        {
            BaudRate = int.Parse(response[1]),
            TXAddress = response[2].Trim().Replace("0x", "").Replace(",", ""),
            RXAddress = response[3].Replace("0x", "").Replace(",", ""),
            Channel = i - 2400,
            CRC = j,
            Rate = int.Parse(response[7]),
            PortName = serial.PortName
        };
         
        return nrf;
    }

    public async Task<ICollection<string[]>> PutConfigurationAsync(NRF24 nrf)
    {
        var worker = _serviceProvider.GetServices<IHostedService>()
                                    .OfType<T>()
                                    .FirstOrDefault();
        
        await worker!.StopAsync(new CancellationToken(true));

        _logger.LogInformation($"Configuring NRF");
        
        List<string> commands = [$"AT+RATE={nrf.Rate}", $"AT+CRC={nrf.CRC}", $"AT+FREQ=2.{nrf.Channel + 400}G", $"AT+TXA={nrf.TXAddress.ToAddressString()}", $"AT+RXA={nrf.RXAddress.ToAddressString()}"];
        
        List<string[]> response = [];
        
        commands.ForEach(command =>
        {
            serial.WriteLine(command);
            response.Add(serial.ReadExisting().ConfigurationResponse());
            _logger.LogInformation($"{serial.ReadExisting()}");
            serial.DiscardInBuffer();

        });
        
        await worker!.StartAsync(new CancellationToken(false));
        
        return response;
    }

    public async Task<string> ReadAsync()
    {
        try
        {
            return await Task.Factory.StartNew(serial.ReadExisting);
        }
        catch (Exception e)
        {
            _logger.LogError($"Error at reading serial port: \n {e}");
            return string.Empty;
        }
    }

    public async Task<string> WriteAsync(string data)
    {
        try
        {
            await Task.Factory.StartNew(() => serial.WriteLine(data));
            return "sent";
        }
        catch (Exception e)
        {
            _logger.LogError($"Error at reading serial port: \n {e}");
            return await Task.FromException<string>(e);
        }
    }

    public void Dispose() => serial.Dispose();

    public async Task StartAsync()
    {
        if (!serial.IsOpen)
        {
            await Task.Factory.StartNew(serial.Open);
        }
    }

    public async Task StopAsync()
    {
        if (serial.IsOpen)
        {
            await Task.Factory.StartNew(serial.Close);
        }

        await Task.Factory.StartNew(this.Dispose); 
    }
}
