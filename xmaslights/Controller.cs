using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;

namespace xmaslights
{
    public class Controller
    {
        
        private readonly List<BackWindow> _windows = new List<BackWindow>();
        private readonly List<Light> _lights = new List<Light>();
        private int _lightsCount;
        private SettingsWindow _tray;
        private Random _rnd;
        private DispatcherTimer _timer;
        private LinearGradientBrush _onBrush;
        private LinearGradientBrush _offBrush;
        
        private int _currentlight = 0;
       private bool skip = false;


        public DispatcherTimer Timer
        {
            get { return _timer; }
        }

        public void Launch()
        {
            if (Properties.Settings.Default.FirstRun)
                SetupFirstRun();

            Properties.Settings.Default.SettingChanging += new System.Configuration.SettingChangingEventHandler(Default_SettingChanging);

            _rnd = new Random();
            _timer = new DispatcherTimer();

            InitializeBrushes();
            CreateSettingsWindow();
            AddChristmasLightsWindow();

            
            StartTimer();            
        }

        private void Default_SettingChanging(object sender, System.Configuration.SettingChangingEventArgs e)
        {
            if (e.SettingName.Equals("TimerEnabled")) 
            {
                _timer.IsEnabled = (bool)e.NewValue;
            } else
            if (e.SettingName.Equals("BlinkPattern"))
            {
                AllLightsOff();
            } else
            if (e.SettingName.Equals("Speed"))
            {
                _timer.Interval = new TimeSpan(0, 0, 0, 0, (int)e.NewValue);
            }
        }
    
        private void CreateSettingsWindow()
        {
            _tray = new SettingsWindow(this);
            _tray.notifyIcon.Visibility = Visibility.Hidden;
        }

        private void StartTimer()
        {
            _timer.Tick += new EventHandler(timer_Tick);
            _timer.Start();
        }

        private void AddChristmasLightsWindow()
        {
            int screenNumber = 0;

            Window alttabhider= new Window()
            {
                Top = -1,
                Left = -1,
                Width = 1,
                Height = 1,
                WindowStyle = WindowStyle.ToolWindow
            };
            alttabhider.Show();
            alttabhider.Hide();

            foreach (WinForms.Screen s in WinForms.Screen.AllScreens)
            {
                BackWindow m = new BackWindow(this)
                {
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = s.Bounds.Left,
                    Top = s.Bounds.Top,
                    Width = s.Bounds.Width,
                    Height = s.Bounds.Height,
                    WindowStyle = WindowStyle.None,
                    Topmost = true,
                    AllowsTransparency = true,
                    Background = Brushes.Transparent,
                    Name = "ChristmasLights_Window" + screenNumber++.ToString()
                };


                m.Owner = alttabhider;
                m.Show();


                // Add left side lights

                Light l;

                RotateTransform leftRotateTransform;
                RotateTransform rightRotateTransform;
                RotateTransform topRotateTransform;

                List<Light> leftSide = new List<Light>();
                List<Light> rightSide = new List<Light>();
                List<Light> topLeftSide = new List<Light>();
                List<Light> topRightSide = new List<Light>();

                for (int x = (Properties.Settings.Default.LightSpacing / 2); x < s.WorkingArea.Height; x += Properties.Settings.Default.LightSpacing)
                {
                    l = new Light();
                    l.OnBrush = _onBrush;
                    l.OffBrush = _offBrush;
                    leftRotateTransform = new RotateTransform(90 + RandomAngle(), 10, 20);
                    l.RenderTransform = leftRotateTransform;
                    Canvas.SetLeft(l, 10);
                    Canvas.SetTop(l, x);

                    leftSide.Insert(0, l);
                    m.lightsCanvas.Children.Add(l);

                    l = new Light();
                    l.OnBrush = _onBrush;
                    l.OffBrush = _offBrush;
                    rightRotateTransform = new RotateTransform(-90 + RandomAngle(), 10, 20);
                    l.RenderTransform = rightRotateTransform;
                    Canvas.SetRight(l, 10);
                    Canvas.SetTop(l, x);

                    rightSide.Add(l);
                    m.lightsCanvas.Children.Add(l);
                }

                for (int y = (Properties.Settings.Default.LightSpacing / 2) + (s.WorkingArea.Width / 2); y < s.WorkingArea.Width; y += Properties.Settings.Default.LightSpacing)
                {
                    l = new Light();
                    l.OnBrush = _onBrush;
                    l.OffBrush = _offBrush;
                    topRotateTransform = new RotateTransform(180 + RandomAngle(), 10, 20);
                    l.RenderTransform = topRotateTransform;
                    Canvas.SetTop(l, 0);
                    Canvas.SetLeft(l, y);
                    topRightSide.Add(l);
                    m.lightsCanvas.Children.Add(l);

                    l = new Light();
                    l.OnBrush = _onBrush;
                    l.OffBrush = _offBrush;
                    l.RenderTransform = topRotateTransform;
                    Canvas.SetTop(l, 0);
                    Canvas.SetRight(l, y);
                    topLeftSide.Insert(0, l);
                    m.lightsCanvas.Children.Add(l);
                }

                _lights.AddRange(leftSide);
                _lights.AddRange(topLeftSide);
                _lights.AddRange(topRightSide);
                _lights.AddRange(rightSide);
                
                m.WindowState = WindowState.Maximized;
                _windows.Add(m);
            }
            _lightsCount = _lights.Count;
            AllLightsOff();
        }

        private void SetupFirstRun()
        {
            Properties.Settings.Default.BlinkPattern = BlinkPattern.Blink;
            Properties.Settings.Default.Speed = 1000;
            Properties.Settings.Default.FirstRun = false;
            Properties.Settings.Default.Save();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            Tick();
        }

        private void Tick()
        {
            _timer.Stop();
            switch (Properties.Settings.Default.BlinkPattern)
            {
                case BlinkPattern.Walking:
                    Running();
                    break;
                case BlinkPattern.Interlaced:
                    Interlaced();
                    break;
                case BlinkPattern.Random:
                    Random();
                    break;
                default:
                    AllOnOff();
                    break;
            }
            _timer.Start();
        }

        
        private void InitializeBrushes()
        {
            _onBrush = new LinearGradientBrush();
            _onBrush.GradientStops = new GradientStopCollection();
            _onBrush.GradientStops.Add(new GradientStop(Colors.Orange, 0.3));
            _onBrush.GradientStops.Add(new GradientStop(Colors.Red, 1));
            _onBrush.Freeze();

            _offBrush = new LinearGradientBrush();
            _offBrush.GradientStops = new GradientStopCollection();
            _offBrush.GradientStops.Add(new GradientStop(Colors.Orange, 0));
            _offBrush.GradientStops.Add(new GradientStop(Colors.Yellow, 1));
            _offBrush.Opacity = 0.5;
            _offBrush.Freeze();
        }

        private void AllOnOff()
        {
           foreach (Light l in _lights)
           {
                l.Switch();
           }
        }

        private void Interlaced()
        {
            skip = !skip;
            foreach (Light l in _lights)
            {
                l.IsOn = skip;
                l.Update();
                skip = !skip;
            }
        }



        private int PreviousLight()
        {
            return (_currentlight -1 + _lightsCount) % _lightsCount;
        }

        private int NextLight()
        {
            return (_currentlight + 1) % _lightsCount;
        }

        private void Running()
        {
            _lights[PreviousLight()].Off(); 
            _lights[NextLight()].On();
            _currentlight = NextLight();
        }

        private void Random()
        {
            _lights[_rnd.Next(_lightsCount - 1)].Switch();
        }
        
        private int RandomAngle()
        {

            return _rnd.Next(-15, 15);

        }

        internal void KeyHit()
        {
            Tick();
        }

        internal void AllLightsOff()
        {
            foreach (Light l in _lights)
            {
                l.Off();
            }
        }
    }
}
