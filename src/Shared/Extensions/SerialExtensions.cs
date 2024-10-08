using Shared.Models;
using System.IO.Ports;

namespace Shared.Extensions;

public static class SerialExtensions
{
    public static async ValueTask<int> ReadAsync(this SerialPort port, byte[] buffer, CancellationToken cancellationToken = default)
    {
        return port.BytesToRead == 0 ? await ValueTask.FromResult(0) : await port.BaseStream.ReadAsync(buffer, cancellationToken);
    }

    public static async ValueTask<string> ReadExistingAsync(this SerialPort port, CancellationToken cancellationToken = default)
    {
        if (port.BytesToRead == 0)
        {
            return string.Empty;
        }

        byte[] buffer = new byte[port.BytesToRead];

        try
        {
            await port.BaseStream.ReadAsync(buffer, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return string.Empty;
        }

        return port.Encoding.GetString(buffer);
    }

    public static async ValueTask<byte> ReadByteAsync(this SerialPort port, CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (port.BytesToRead > 0)
                break;
            await Task.Delay(100, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        byte[] buffer = new byte[1];

        await port.BaseStream.ReadAsync(buffer, cancellationToken);

        return buffer[0];
    }

    public static async ValueTask<string> ReadStringAsync(this SerialPort port, CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (port.BytesToRead > 0)
                break;
            await Task.Delay(100, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        byte[] buffer = new byte[port.BytesToRead];

        await port.BaseStream.ReadAsync(buffer, cancellationToken);

        string read = port.Encoding.GetString(buffer);
        
        if (read.Contains("\n"))
            read = read.Substring(0, read.IndexOf("\n"));
        
        return read;
    }

    public static async ValueTask<byte[]> ReadBytesAsync(this SerialPort port, CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (port.BytesToRead > 0)
                break;
            await Task.Delay(100, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        byte[] buffer = new byte[port.BytesToRead];

        await port.BaseStream.ReadAsync(buffer, cancellationToken);

        return buffer;
    }

    public static async ValueTask<char> ReadCharAsync(this SerialPort port, CancellationToken cancellationToken = default)
    {
        int byte_count = port.Encoding.GetMaxByteCount(1);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (port.BytesToRead >= byte_count)
                break;
            await Task.Delay(100, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        byte[] buffer = new byte[byte_count];
        await port.BaseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        return port.Encoding.GetChars(buffer)[0];
    }

    public static async ValueTask<string> ReadLineAsync(this SerialPort port, CancellationToken cancellationToken = default)
    {

        while (!cancellationToken.IsCancellationRequested)
        {
            if (port.BytesToRead > 0)
                break;
            await Task.Delay(100, cancellationToken);
        }
        byte[] buffer = new byte[port.BytesToRead];
       
        await port.BaseStream.ReadAsync(buffer, cancellationToken);
        string result = port.Encoding.GetString(buffer);
        result = result.Contains(port.NewLine) ? result.Replace(result.Substring(result.IndexOf(port.NewLine)), "") : result;
        return result;
    }

    public static async ValueTask<string> ReadToAsync(this SerialPort port, string value, CancellationToken cancellationToken = default)
    {
        int byte_count = port.Encoding.GetMaxByteCount(1);

        byte[] buffer = new byte[byte_count];

        string result = "";

        while (!cancellationToken.IsCancellationRequested)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (port.BytesToRead >= byte_count)
                    break;
                await Task.Delay(100, cancellationToken);
            }

            await port.BaseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

            char extracted_char = port.Encoding.GetChars(buffer)[0];

            result += extracted_char;

            if (result.EndsWith(value, StringComparison.InvariantCultureIgnoreCase))
                break;
        }

        result = result.Remove(result.Length - value.Length);

        return result;
    }

    public static ValueTask<int> ReadAsync(this SerialPort port, Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return buffer.Length == 0 ? ValueTask.FromResult(0) : port.BaseStream.ReadAsync(buffer, cancellationToken);
    }

    public static Task<int> ReadAsync(this SerialPort port, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        return port.BaseStream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public static ValueTask WriteAsync(this SerialPort port, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return buffer.Length == 0 ? ValueTask.CompletedTask : port.BaseStream.WriteAsync(buffer, cancellationToken);
    }

    public static ValueTask WriteAsync(this SerialPort port, byte[] buffer, CancellationToken cancellationToken = default)
    {
        return buffer.Length == 0 ? ValueTask.CompletedTask : port.BaseStream.WriteAsync(buffer, cancellationToken); 
    }

    public static async Task WriteAsync(this SerialPort port, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        await port.BaseStream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
    }

    public static async ValueTask WriteLineAsync(this SerialPort port, string value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }
        value += Environment.NewLine;
        await port.BaseStream.WriteAsync(port.Encoding.GetBytes(value), cancellationToken);
    }
}
