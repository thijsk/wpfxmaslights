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
        private SettingsWindow _tray;

        public void Launch()
        {
            if (Properties.Settings.Default.FirstRun)
                SetupFirstRun();

            CreateSettingsWindow();
            AddChristmasLightsWindow();

            StartTimer();
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
        }

        private void CreateSettingsWindow()
        {
            _tray = new SettingsWindow(this);
            _tray.notifyIcon.Visibility = Visibility.Hidden;
        }

        private DispatcherTimer _timer = new DispatcherTimer();

        public DispatcherTimer Timer 
        {
            get { return _timer; }
        }

        private void StartTimer()
        {
           // _timer.Dispatcher.Thread.Priority = System.Threading.ThreadPriority.Lowest;
            _timer.Tick += new EventHandler(timer_Tick);
            _timer.Start();
        }

        private void AddChristmasLightsWindow()
        {
            int screenNumber = 0;

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

                m.Show();

                const int LIGHTSPACING = 200;

                // Add left side lights

                Light l;

                RotateTransform leftRotateTransform;
                RotateTransform rightRotateTransform;
                RotateTransform topRotateTransform;

                List<Light> leftSide = new List<Light>();
                List<Light> rightSide = new List<Light>();
                List<Light> topLeftSide = new List<Light>();
                List<Light> topRightSide = new List<Light>();

                for (int x = (LIGHTSPACING / 2); x < s.WorkingArea.Height; x += LIGHTSPACING)
                {
                    l = new Light();
                    leftRotateTransform = new RotateTransform(90 + RandomAngle(), 10, 20);
                    l.RenderTransform = leftRotateTransform;
                    Canvas.SetLeft(l, 10);
                    Canvas.SetTop(l, x);

                    leftSide.Insert(0, l);
                    m.lightsCanvas.Children.Add(l);

                    l = new Light();
                    rightRotateTransform = new RotateTransform(-90 + RandomAngle(), 10, 20);
                    l.RenderTransform = rightRotateTransform;
                    Canvas.SetRight(l, 10);
                    Canvas.SetTop(l, x);

                    rightSide.Add(l);
                    m.lightsCanvas.Children.Add(l);
                }

                for (int y = (LIGHTSPACING / 2) + (s.WorkingArea.Width / 2); y < s.WorkingArea.Width; y += LIGHTSPACING)
                {
                    l = new Light();
                    topRotateTransform = new RotateTransform(180 + RandomAngle(), 10, 20);
                    l.RenderTransform = topRotateTransform;
                    Canvas.SetTop(l, 0);
                    Canvas.SetLeft(l, y);
                    topRightSide.Add(l);
                    m.lightsCanvas.Children.Add(l);

                    l = new Light();
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
        }

        private void SetupFirstRun()
        {
           // Properties.Settings.Default.BlinkPattern = BlinkPattern.Walking;
            Properties.Settings.Default.FirstRun = false;
            Properties.Settings.Default.Save();
        }

        void timer_Tick(object sender, EventArgs e)
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
                default:
                    AllOnOff();
                    break;
            }
            _timer.Start();
        }

        private void AllOnOff()
        {
           foreach (Light l in _lights)
            {
                l.Blink();
                 //if (!on)
                 //    l.On();
                 //else
                 //   l.Off();
            }
            //on = !on;
        }

        private bool skip = false;
        private void Interlaced()
        {
            skip = !skip;
            foreach (Light l in _lights)
            {
                l.on = skip;
                l.Blink();
                skip = !skip;
            }
        }

        private int currentlight = 0;
        private void Running()
        {
            _lights[currentlight].Blink();
            _lights[(currentlight + 3) % _lights.Count].Blink();
            _lights[(currentlight + 6) % _lights.Count].Blink();
            _lights[(currentlight + 9) % _lights.Count].Blink();
            if (++currentlight >= _lights.Count)
                currentlight = 0;
        }
        
        private Random _rnd;
        private int RandomAngle()
        {
            if (_rnd == null)
                _rnd = new Random();

            return _rnd.Next(-15, 15);

        }

        internal void KeyHit()
        {
            timer_Tick(null, null);
        }

        internal void AllLightsOff()
        {
            foreach (Light l in _lights)
            {
                l.on = false;
            }
        }
    }
}
