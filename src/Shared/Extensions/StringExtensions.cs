namespace Shared.Extensions;

public static class StringExtensions
{ 
    public static int IndexOfChar (this string value, char c)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(value, nameof(value));
        ArgumentNullException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        
        return Array.IndexOf(value.ToCharArray(), c);   
    } 
}

