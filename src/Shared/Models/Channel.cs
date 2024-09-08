using Microsoft.Extensions.DependencyInjection;
using System.Buffers.Binary;
using System.Text;
using System.Text.RegularExpressions;

namespace Shared.Models;

public class Channel
{
    public int[] Value { get; set; } = new int[8];
    public string HexValue { get { return Encode(this); } set { this.HexValue = value; } }


    public string Encode(Channel channel)
    {
        ArgumentNullException.ThrowIfNull(channel, nameof(channel));

        ArgumentOutOfRangeException.ThrowIfNotEqual(channel.Value.Length, 8, nameof(channel.Value.Length));

        channel.Value.ToList().ForEach(value =>
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1000, nameof(value));

            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 2000, nameof(value));
        });

        StringBuilder builder = new();

        channel.Value.ToList().ForEach(async value =>
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

    public Channel Decode(string data)
    {
        var pattern = @"^[A-Fa-f0-9]{24}$";

        if (!Regex.IsMatch(pattern, data))
        {
            throw new ArgumentException("Invalid data", nameof(data));
        }

        var channel = new Channel();

        for (int i = 0; i < data.Length / 3; i++)
        {
            var hex = data.Substring(i * 3, 3);
            var success = int.TryParse(hex, out int result);
            if (success)
            {
                channel.Value[i] = result;
            }
        }

        return channel;
    }


}

