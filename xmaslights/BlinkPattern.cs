using System;

namespace xmaslights
{   
    [Flags]
    public enum BlinkPattern
    {
        Blink,
        Walking,
        Interlaced,
        Random,
        KnightRider
    }
}
