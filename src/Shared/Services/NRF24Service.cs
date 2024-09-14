using Microsoft.Extensions.Logging;
using Shared.Models;
using Shared.Extensions;
using System.IO.Ports;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using Microsoft.Extensions.Options;
using System.Text;
using System.Reflection.Metadata.Ecma335;

namespace Shared.Services;

public class NRF24Service<T> : INRF24Service
    where T : IBGTxRx
{
    private readonly NRF24 _nrf;

    private readonly IServiceScopeFactory _factory;

    private readonly List<int> _bauds = [4800, 9600, 14400, 19200, 38400, 57600, 115200];

    private readonly List<int> _crc = [8, 16];

    private readonly List<int> _rates = [250, 1, 2];

    private readonly IServiceProvider _serviceProvider;

    private readonly ILogger<NRF24Service<T>> _logger;

    private static SerialPort serial = new();
    public Encoding Encoding => serial.Encoding;
    public bool NRFPortIsOpen => serial.IsOpen;
    public NRF24 NRF24 { get; set; } = default!;

    public string[] Ports => SerialPort.GetPortNames();

    public event SerialDataReceivedEventHandler SerialDataReceived = default!;

    public event SerialErrorReceivedEventHandler SerialErrorReceived = default!;
    public int BytesToRead => serial.BytesToRead;

    public NRF24Service(IServiceProvider serviceProvider, IOptions<NRF24> nrf, ILogger<NRF24Service<T>> logger, IServiceScopeFactory factory)
    {
        _factory = factory;

        _nrf = nrf.Value;

        _logger = logger;

        _serviceProvider = serviceProvider;

        try
        {
            using AsyncServiceScope asyncScope = _factory.CreateAsyncScope();
            var _nrfValidator = asyncScope.ServiceProvider.GetRequiredService<IValidator<NRF24>>();
            var results = _nrfValidator.ValidateAsync(_nrf).GetAwaiter().GetResult();

            if (!results.IsValid)
            {
                _logger.LogCritical("Invalid radio Configuration");

                throw new InvalidDataException("Invalid radio configuration");
            }

            if (!Ports.Contains(_nrf.Port))
            {
                if (Ports.Length > 1)
                {
                    _nrf.Port = Ports.Last();
                }
            }

            if (serial.IsOpen)
            {
                return;
            }
            serial.PortName = _nrf.Port;
            serial.BaudRate = _bauds[_nrf.BaudRate - 1];
            serial.DataBits = 8;
            serial.Parity = Parity.None;
            serial.StopBits = StopBits.One;
            serial.Encoding = Encoding.UTF8;
            serial.DataReceived += OnReceived;
            serial.ErrorReceived += OnError;
            _logger.LogInformation($"opening serial port {_nrf.Port} with baud {serial.BaudRate}");
            serial.Open();

        }
        catch (Exception e)
        {
            _logger.LogError($"Error while activating services {e}");
        }
    }

    private void OnError(object sender, SerialErrorReceivedEventArgs e)
    {
        SerialErrorReceived?.Invoke(sender, e);
    }

    private void OnReceived(object sender, SerialDataReceivedEventArgs e)
    {
        SerialDataReceived?.Invoke(sender, e);
    }

    public async Task<NRF24> GetConfigurationAsync()
    {
        var _sp = _serviceProvider.CreateScope();
        var bg = _sp.ServiceProvider.GetService<T>();

        bg!.IsEnabled = false;
        serial.DiscardInBuffer();
        await serial.WriteLineAsync($"AT?");

        while (serial.BytesToRead <= 213)
        {
            continue;
        }
        var response = (await serial.ReadExistingAsync()).ConfigurationResponse();

        serial.DiscardInBuffer();

        ArgumentNullException.ThrowIfNull(response, nameof(response));

        ArgumentOutOfRangeException.ThrowIfLessThan(response.Length, 8, nameof(response.Length));

        _ = int.TryParse(response[4].Replace(".", "").Replace("GHz", ""), out int i);

        _ = int.TryParse(response[5].Replace("CRC", ""), out int j);

        NRF24 nrf = new()
        {
            BaudRate = int.Parse(response[1]),
            TXAddress = response[2].Trim(),
            RXAddress = response[3].StartsWith("00") ? response[3].Remove(0, 1) : response[3],
            Channel = i - 2400,
            CRC = j,
            Rate = int.Parse(response[7].Contains("Kbps") ? response[7].Replace("Kbps", "") : response[7].Contains("Mbps") ? response[7].Replace("Mbps", "") : response[7]),
            Port = serial.PortName
        };

        bg.IsEnabled = true;

        return nrf;
    }

    public async Task<ICollection<string>> PutConfigurationAsync(NRF24 nrf)
    {

        var _sp = _serviceProvider.CreateScope();
        var bg = _sp.ServiceProvider.GetService<T>();

        bg!.IsEnabled = false;
        _logger.LogInformation($"Configuring NRF");

        List<string> commands = [$"AT+BAUD={_bauds.IndexOf(nrf.BaudRate) + 1}", $"AT+RATE={_rates.IndexOf(nrf.Rate) + 1}", $"AT+CRC={nrf.CRC}", $"AT+FREQ=2.{nrf.Channel + 400}G", $"AT+TXA={nrf.TXAddress}", $"AT+RXA={nrf.RXAddress}"];
        serial.DiscardInBuffer();

        List<string> response = [];
        foreach (string cmd in commands)
        {
            await serial.WriteLineAsync(cmd);
            Thread.Sleep(150);
        }
        var read = await serial.ReadExistingAsync();
        var ok = read?.ConfigurationResponse()!;
        response.AddRange(ok);

        serial.DiscardInBuffer();

        bg!.IsEnabled = true;

        return response;
    }

    public async Task<string> ReadToAsync(string to = "\n")
    {
        try
        {
            return await serial.ReadToAsync(to);
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<string> ReadAsync(string to = "\n")
    {
        try
        {
            string read = string.Empty;
            while (serial.BytesToRead > 0)
            {
                read += serial.Encoding.GetString(await serial.ReadBytesAsync());
                if (read.EndsWith("\n"))
                    read.Remove(read.Length - 1);
                return read;
            }

            if (read.Contains(to))
            {
                return read.Split('\n').FirstOrDefault(x => x.Length == 24)!;
            }
            return read;
        }
        catch (Exception e)
        {
            _logger.LogError($"Error at reading serial port: \n {e}");
            return string.Empty;
        }
    }
    public async Task<string> ReadLineAsync()
    {
        try
        {
            //return serial.Encoding.GetString([await serial.ReadByteAsync()]);
            return await serial.ReadLineAsync();
        }
        catch (Exception e)
        {
            _logger.LogError($"Error at reading serial port: \n {e}");
            return string.Empty;
        }
    }

    public async Task<byte> ReadByteAsync()
    {
        try
        {
            //return serial.Encoding.GetString([await serial.ReadByteAsync()]);
            return await serial.ReadByteAsync();
        }
        catch (Exception e)
        {
            _logger.LogError($"Error at reading serial port: \n {e}");
            return 0x0;
        }
    }

    public async Task<string> WriteAsync(string data)
    {
        try
        {
            await serial.WriteLineAsync(data);
            return "sent";
        }
        catch (Exception e)
        {
            _logger.LogError($"Error at writing to serial port: \n {e}");
            return await Task.FromException<string>(e);
        }
    }

    public void DiscardInputBuffer()
    {
        if (this.NRFPortIsOpen)
        {
            serial.DiscardInBuffer();
        }
    }
}
