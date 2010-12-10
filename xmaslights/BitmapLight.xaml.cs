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
using System.Windows.Threading;

namespace xmaslights
{
    /// <summary>
    /// Interaction logic for Light.xaml
    /// </summary>
    public partial class BitmapLight : UserControl, ILight
    {
        private static BitmapImage lightRed;
        private static BitmapImage lightYellow;
        private static BitmapImage lightBroken;
        private static Random random = new Random();
        private static MediaPlayer player;

        static BitmapLight()
        {
            lightRed = new BitmapImage(new Uri("pack://application:,,,/ChristmasLights;Component/Resources/LightRedSmall.png"));
            lightYellow = new BitmapImage(new Uri("pack://application:,,,/ChristmasLights;Component/Resources/LightYellowSmall.png"));
            lightBroken = new BitmapImage(new Uri("pack://application:,,,/ChristmasLights;Component/Resources/LightBrokenSmall.png"));

            RenderOptions.SetCachingHint(lightRed, CachingHint.Cache);
            RenderOptions.SetCachingHint(lightYellow, CachingHint.Cache);
            RenderOptions.SetCachingHint(lightBroken, CachingHint.Cache);

            lightRed.Freeze();
            lightYellow.Freeze();
            lightBroken.Freeze();
            
            player = new MediaPlayer();
            player.Volume = 1;
            player.Open(new Uri(@"Resources\glass_break_1.mp3", UriKind.Relative));
            player.MediaEnded += new EventHandler(delegate(object o, EventArgs e) { player.Stop(); });
            player.Stop();

        }

        public BitmapLight()
        {
            InitializeComponent();
            InitLight();
        }

        private bool _isOn;
        private int _clickCounter;
        private int _blinkCounter;
        private int _lifetime;
        private bool _isBroken;
        private DispatcherTimer _repairtimer;
       
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
            _blinkCounter++;
            if (!_isBroken)
            {
           
                 if (_lifetime == _blinkCounter)
                {
                    Break();
                    return;
                }

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
            if (!_isBroken && ++_clickCounter == 4)
            {
                Break();
            }
        }

        private void Break()
        {
            player.Play();
            _isBroken = true;
            _repairtimer = new DispatcherTimer(new TimeSpan(0,0,20), DispatcherPriority.ApplicationIdle, new EventHandler(delegate(object o, EventArgs e) { Repair(); }), Dispatcher.CurrentDispatcher);
            _repairtimer.Start();
            this.lightImage.Source = lightBroken;
        }

        private void Repair()
        {
            _repairtimer.Stop();
            InitLight();
        }

        private void InitLight()
        {
            _isBroken = false;
            _clickCounter = 0;
            _blinkCounter = 0;
            _lifetime = random.Next(3600, 10800);
        }
    }
}
