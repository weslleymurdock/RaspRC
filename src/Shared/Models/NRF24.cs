namespace Shared.Models;

public class NRF24 
{ 
    public int Id { get; set; }
     
    public string PortName { get; set; } = "/dev/ttyUSB0";
     
    public int BaudRate { get; set; } = 2;
     
    public int Rate { get; set; } = 1;
     
    public int Channel { get; set; } = 510;
     
    public int CRC { get; set; } = 16;
     
    public string RXAddress { get; set; } = "FFFFFFFFFF";
     
    public string TXAddress { get; set; } = "FFFFFFFFFF";
}
