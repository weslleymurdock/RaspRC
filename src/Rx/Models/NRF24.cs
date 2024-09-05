namespace Rx.Models;

public class NRF24 
{
    public string RX { get; set; } = string.Empty;
    public string TX { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
    public int BaudRate { get; set; } = 9600;
    public int Rate { get; set; } = 1;
    public int Freq { get; set; } = 510;
    public int CRC { get; set; } = 16;
}
