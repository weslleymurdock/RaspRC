using Shared.Models;
using System.IO.Ports;

namespace Shared.Services;

public interface INRF24Service
{
    NRF24 NRF24 { get; set; }
    bool NRFPortIsOpen { get; }
    string[] Ports { get; }

    void DiscartInputBuffer();
    Task<NRF24> GetConfigurationAsync();

    Task<ICollection<string>> PutConfigurationAsync(NRF24 nrf);

    Task<string> ReadAsync();

    Task<string> WriteAsync(string data);
     
    event SerialDataReceivedEventHandler SerialDataReceived;

    event SerialErrorReceivedEventHandler SerialErrorReceived;
}
