using System.Text;
using System.Text.RegularExpressions;

namespace Shared.Extensions;

public static class NRFExtensions
{
    public static string[] ConfigurationResponse(this string value) => Encoding.ASCII.GetString(
            Encoding.Convert(
                Encoding.UTF8,
                Encoding.GetEncoding(
                    Encoding.ASCII.EncodingName,
                    new EncoderReplacementFallback(string.Empty),
                    new DecoderExceptionFallback()),
                Encoding.UTF8.GetBytes(value)))
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

    public static string ToAddressString(this string value)
    {
        if (!Regex.IsMatch(value, @"^[A-Fa-f0-9]{10}$"))
        {
            throw new InvalidDataException("The address must be an hex string with exactly 10 hex digits");
        }
        StringBuilder address = new("0x");
        for (int i = 0; i < value.Length / 2; i++)
        {
            address.Append(value.AsSpan(i * 2, 2));
            if ((address.Length/2) - 1 != i)
            {
                address.Append(",0x");
            }
        }
        return address.ToString();
    }

    public static decimal Map(this decimal value, decimal initialMin, decimal initialMax, decimal finalMin = 1000, decimal finalMax = 2000)
    {
        return (value - initialMin) / (initialMax - initialMin) * (finalMax - finalMin) + finalMin;
    }
}
