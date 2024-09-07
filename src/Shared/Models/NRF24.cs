using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Models;

public record NRF24(string portName, int baudRate, int Channel, int Rate, int CRC, string rxAddress, string txAddress)
{
    [Key]
    [Column(Order=1)]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required, MinLength(4)]
    [Column(Order=2)]
    [StringLength(100)]
    public string PortName { get; set; }

    [Required, MinLength(1)]
    [Column(Order=3)]
    [Range(1, 6, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int BaudRate { get; set; }

    [Required, MinLength(1), MaxLength(1)]
    [Column(Order=5)]
    [Range(1, 3, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Rate { get; set; }

    [Required, MinLength(1), MaxLength(3)]
    [Column(Order=4)]
    [Range(0, 125, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Channel { get; set; }

    [Column(Order=6)]
    [Required, MinLength(1), MaxLength(2)]
    [Range(8, 16)]
    public int CRC { get; set; }

    [Required, MinLength(10), MaxLength(10)]
    [Column(Order=7)]
    [RegularExpression(@"^[0-9A-F]{10}$",
         ErrorMessage = "Only 5 hexadecimal values are allowed.")]
    public string RXAddress { get; set; }

    [Required, MinLength(10), MaxLength(10)]
    [Column(Order=8)]
    [RegularExpression(@"^[0-9A-F]{10}$",
         ErrorMessage = "Only 5 hexadecimal values are allowed.")]
    public string TXAddress { get; set; }
}
