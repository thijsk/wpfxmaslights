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
            InitializeStoryboards();
        }

        private void InitializeStoryboards()
        {
           // blinkStoryboard = (Storyboard)TryFindResource("LightBlink");
          //  blinkStoryboard.SpeedRatio = Properties.Settings.Default.FadeSpeedRatio;
            
        }

        public bool on = true;

        public void Blink()
        {
            on = !on;
            
            if (on)
            {
                ((LinearGradientBrush)this.path.Fill).GradientStops[0].Color = Colors.Orange;
                ((LinearGradientBrush)this.path.Fill).GradientStops[1].Color = Colors.Red;
                ((LinearGradientBrush)this.path.Fill).Opacity = 1;
            }
            else
            {
                ((LinearGradientBrush)this.path.Fill).GradientStops[0].Color = Colors.Yellow;
                ((LinearGradientBrush)this.path.Fill).GradientStops[1].Color = Colors.White;
                ((LinearGradientBrush)this.path.Fill).Opacity = 0.5;
            }
        }

    }
}
