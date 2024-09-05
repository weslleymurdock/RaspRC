using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rx.Models;

public class Receiver
{
    public NRF24 NRF24 { get; set; } = default(NRF24);
}
