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
using System.ComponentModel;

namespace xmaslights
{
    /// <summary>
    /// Interaction logic for Light.xaml
    /// </summary>
    public partial class BitmapLight : UserControl, ILight
    {
        private static BitmapImage lightRed = new BitmapImage(new Uri("pack://application:,,,/ChristmasLights;Component/Resources/LightRedSmall.png"));
        private static BitmapImage lightYellow = new BitmapImage(new Uri("pack://application:,,,/ChristmasLights;Component/Resources/LightYellowSmall.png"));
        private static BitmapImage lightBroken = new BitmapImage(new Uri("pack://application:,,,/ChristmasLights;Component/Resources/LightBrokenSmall.png"));

        static BitmapLight()
        {
            RenderOptions.SetCachingHint(lightRed, CachingHint.Cache);
            RenderOptions.SetCachingHint(lightYellow, CachingHint.Cache);
            lightRed.Freeze();
            lightYellow.Freeze();
        }

        public BitmapLight()
        {
            InitializeComponent();
            _isOn = false;
            _isBroken = false;
        }

        private bool _isOn;
        private int teller = 0;
        private bool _isBroken;

        public void Switch()
        {
            _isOn = !_isOn;
            Update();
        }

        public void On()
        {
            _isOn = true;
            Update();
        }

        public void Off()
        {
            _isOn = false;
            Update();
        }

        public void Update()
        {
            if (!_isBroken)
            { 
                if (_isOn)
                {
                    this.lightImage.Source = lightYellow;
                }
                else
                {
                    this.lightImage.Source = lightRed;
                }
            }
        }

        public void Rotate(int angle)
        {
            this.RenderTransform = new RotateTransform(angle, 10, 20);
        }

        public bool IsOn()
        {
            return _isOn;
        }

     

        public void Click()
        {
            teller++;
            if (teller == 5)
            {
                this.lightImage.Source = lightBroken;
                _isBroken = true;
            }
        }
    }
}
