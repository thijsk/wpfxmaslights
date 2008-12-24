using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace xmaslights
{   
    [Flags]
    public enum BlinkPattern
    {
        Blink,
        Walking,
        Interlaced,
        Random
    }
}
