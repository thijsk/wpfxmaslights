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
        private Storyboard blinkStoryboard;

        public Light()
        {
            InitializeComponent();
            InitializeStoryboards();
        }

        private void InitializeStoryboards()
        {
            blinkStoryboard = (Storyboard)TryFindResource("LightBlink");
            blinkStoryboard.SpeedRatio = Properties.Settings.Default.FadeSpeedRatio;
            
        }

        //public void On()
        //{
        //    blinkStoryboard.AutoReverse = false;
        //    blinkStoryboard.Begin(this);
        //}


        //public void Off()
        //{
        //    blinkStoryboard.Seek(new TimeSpan(0,0,0), TimeSeekOrigin.Duration);
        //    blinkStoryboard.AutoReverse = true;
        //    blinkStoryboard.Begin();
        //}

        private bool on = true;

        public void Blink()
        {
            //blinkStoryboard.Stop();
            //blinkStoryboard.AutoReverse = true;
            //blinkStoryboard.Begin();
            on = !on;
            
            ((DropShadowEffect)this.path.Effect).Opacity = on ? 1 : 0;
        }

    }
}
