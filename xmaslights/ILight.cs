using System.Windows.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows;

namespace xmaslights
{
    public interface ILight
    {
        void Off();

        void On();

        void Switch();

        void Rotate(int angle);

        bool IsOn();
    }
}
