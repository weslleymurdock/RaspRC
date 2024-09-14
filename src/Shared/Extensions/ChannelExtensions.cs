using System.Text;
using System.Text.RegularExpressions;

namespace Shared.Extensions;

public static partial class ChannelExtensions
{
    public static string FillWithComma(this string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!HexValue().IsMatch(value))
        {
            throw new InvalidDataException("The Value does not match a hex value with 24 digits");
        }
        StringBuilder sb = new StringBuilder();

        for (var i = 0; i < 8; i++)
        {
            sb.Append(value.Substring(i * 3, 3));
            if (i < 7)
                sb.Append(",");
        }
        return sb.ToString();
    }

    [GeneratedRegex("^[A-Fa-f0-9]{24}$")]
    private static partial Regex HexValue();
}
