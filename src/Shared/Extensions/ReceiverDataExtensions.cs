using Shared.Models;

namespace Shared.Extensions;

public static class ReceiverDataExtensions 
{
    ///<summary>
    /// Maps a value from one arbitrary range to another arbitrary range
    ///</summary>
    public static char MapChar(this char value, char fromMin, char fromMax, char toMin, char toMax) => 
        ((char)((int)toMin + ((int) value - (int)fromMin) * ((int)toMax - (int)toMin) / ((int)fromMax - (int)fromMin)));

}
