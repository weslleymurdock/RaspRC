namespace Shared.Models;

public class NRF24 
{  
    public string? PortName { get; set; } 
     
    public int BaudRate { get; set; }  
     
    public int Rate { get; set; } 
     
    public int Channel { get; set; }  
     
    public int CRC { get; set; } 
     
    public string? RXAddress { get; set; } 
     
    public string? TXAddress { get; set; }  
}
