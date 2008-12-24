using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace xmaslights
{
    /// <summary>
    /// Interaction logic for Light.xaml
    /// </summary>
    public partial class Light : UserControl
    {
        //private Storyboard blinkStoryboard;

        public Light()
        {
            InitializeComponent();
            IsOn = false;
        }

        public bool IsOn { get; set; }
        public Brush OnBrush { get; set; }
        public Brush OffBrush { get; set; }

        public void Switch()
        {
            IsOn = !IsOn;
            Update();
        }

        public void On()
        {
            IsOn = true;
            Update();
        }

        public void Off()
        {
            IsOn = false;
            Update();
        }

        public void Update()
        {
            if (IsOn)
                this.path.Fill = OnBrush;
            else
                this.path.Fill = OffBrush;
        }

    }
}
