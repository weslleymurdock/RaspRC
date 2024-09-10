using Microsoft.Extensions.DependencyInjection;
using System.Buffers.Binary;
using System.Text;
using System.Text.RegularExpressions;

namespace Shared.Models;

public class Channel
{
    public int[] Value { get; set; } = new int[8];
    public string HexValue { get; set; } = string.Empty;

    public Channel()
    {

    }

    public Channel(int[] values)
    {
        Value = values;
        HexValue = Encode(values);
    }

    public string Encode(int[] values)
    {
        ArgumentNullException.ThrowIfNull(values, nameof(values));

        ArgumentOutOfRangeException.ThrowIfNotEqual(values.Length, 8, nameof(values.Length));

        values.ToList().ForEach(value =>
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1000, nameof(value));

            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 2000, nameof(value));
        });

        StringBuilder builder = new();

        values.ToList().ForEach(async value =>
        {
            try
            {
                byte[] b = new byte[4];
                BinaryPrimitives.WriteInt32BigEndian(b, value);
                builder.Append(Convert.ToHexString(b).Remove(0, 5));
            }
            catch (Exception e)
            {
                _ = await Task.FromException<string>(e);
            }
        });

        return builder.ToString();
    }

    public int[] Decode(string data)
    {
        var pattern = @"^[A-Fa-f0-9]{24}$";

        if (!Regex.IsMatch(pattern, data))
        {
            throw new ArgumentException("Invalid data", nameof(data));
        }

        var values = new int[8];

        for (int i = 0; i < data.Length / 3; i++)
        {
            var hex = data.Substring(i * 3, 3);
            var success = int.TryParse(hex, out int result);
            if (success)
            {
                values[i] = result;
            }
        }

        return values;
    }


}

