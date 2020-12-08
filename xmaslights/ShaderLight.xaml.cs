using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HueShift;

namespace xmaslights
{
    /// <summary>
    /// Interaction logic for ShaderLight.xaml
    /// </summary>
    public partial class ShaderLight : ILight
    {
        private static BitmapImage lightOff;
        private static BitmapImage lightOn;
        private static BitmapImage lightBroken;
        private static Random random = new Random();
        private static MediaPlayer player;

        static ShaderLight()
        {
            lightOn = new BitmapImage(new Uri("pack://application:,,,/ChristmasLights;Component/Resources/LightYellowSmall.png"));
            RenderOptions.SetCachingHint(lightOn, CachingHint.Cache);
            lightOn.Freeze();

            lightOff = new BitmapImage(new Uri("pack://application:,,,/ChristmasLights;Component/Resources/LightRedSmall.png"));
            RenderOptions.SetCachingHint(lightOff, CachingHint.Cache);
            lightOff.Freeze();

            lightBroken = new BitmapImage(new Uri("pack://application:,,,/ChristmasLights;Component/Resources/LightBrokenSmall.png"));
            RenderOptions.SetCachingHint(lightBroken, CachingHint.Cache);
            lightBroken.Freeze();
            
            player = new MediaPlayer();
            player.Volume = 1;
            player.Open(new Uri(@"Resources\glass_break_1.mp3", UriKind.Relative));
            player.MediaEnded += delegate { player.Stop(); };
            player.Stop();

        }

        public ShaderLight()
        {
            InitializeComponent();
            InitLight();
        }

        public  double HueShift { get; set; }

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
                    lightImage.Source = lightOn;
                    ((ShiftHueEffect)lightImage.Effect).HueShift = HueShift;
                }
                else
                {
                    lightImage.Source = lightOff;
                    ((ShiftHueEffect)lightImage.Effect).HueShift = HueShift;
                }
            }
        }

        public void Rotate(int angle)
        {
            RenderTransform = new RotateTransform(angle, 10, 20);
        }

        public bool IsOn()
        {
            return _isOn;
        }

        public void Click()
        {
            if (!_isBroken && ++_clickCounter == 3)
            {
                Break();
            }
        }

        private void Break()
        {
            player.Play();
            _isBroken = true;
            _repairtimer = new DispatcherTimer(new TimeSpan(0,0,20), DispatcherPriority.ApplicationIdle, delegate { Repair(); }, Dispatcher.CurrentDispatcher);
            _repairtimer.Start();
            lightImage.Source = lightBroken;
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
            ShiftHueEffect effect = new ShiftHueEffect();
            lightImage.Effect = effect;
            lightImage.Source = lightOn;
        }
    }
}
