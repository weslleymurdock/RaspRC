namespace Shared.Extensions;

public static class NRFExtensions
{
    public static string ToAddressString(this byte[] bytes)
    {
        var address = new System.Numerics.BigInteger(bytes.Reverse().ToArray()).ToString("x2");
        string pattern = @"^([0][x]([A-Fa-f0-9]){2})+([,][0][x]([A-Fa-f0-9]){2}){4}$";
        var @return = string.Empty;
        for (int i = 0; i < address.Length ; i += 2) 
        {
            if (i == 0)
            {
                @return += $"0x{address.Substring(i, i + 2)}";
                continue;
            }
            if (i + 2 == address.Length)
            {
                @return += $",0x{address.Substring(i)}";
            }
            else
            {
                @return += $",0x{address.Substring(i, i + 2)}";
            }
        }

        return @return;
    }
}
