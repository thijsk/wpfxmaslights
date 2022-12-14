using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;
using Microsoft.Win32;
using System.Threading;
using xmaslights.Properties;

namespace xmaslights
{
    public class Controller
    {
        private readonly List<BackWindow> _windows = new List<BackWindow>();
        private int _lightSpacing;
        private readonly Random _rnd;
        private DispatcherTimer _timer;
        private DateTime _lastShuffle;
        private TimeSpan _timerInterval;
        private readonly Dispatcher _dispatcher;
        private WinForms.NotifyIcon _trayIcon;
     
        private bool _timerEnabled;
        private double _lightHueShift;

        private bool _tickEnabled;
        
        public Controller()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _rnd = new Random();
           
            Settings.Default.SettingChanging += Default_SettingChanging;
            Settings.Default.SettingsLoaded += Default_SettingsLoaded;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            SystemEvents.DisplaySettingsChanging += SystemEvents_DisplaySettingsChanging;
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        ~Controller()
        {
            Settings.Default.SettingChanging -= Default_SettingChanging;
            Settings.Default.SettingsLoaded -= Default_SettingsLoaded;
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            SystemEvents.DisplaySettingsChanging -= SystemEvents_DisplaySettingsChanging;
            SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;

        }

        public void Start()
        {
            LoadSettings();
            
            AddChristmasLightsWindows();
            PopulateWindows(false);

            StartStopTickGenerators();
            _tickEnabled = true;

            if (Settings.Default.FirstRun)
                SetupFirstRun();

            CreateTrayIcon();
        }

        private void StartStopTickGenerators()
        {
            StartStopTimer();
        }

        private void LoadSettings()
        {
            _lightSpacing = Settings.Default.LightSpacing;
            _timerEnabled = Settings.Default.TimerEnabled;
           
            _lightHueShift = Settings.Default.LightHueShift;
        }

        public void Stop()
        {
            DestroyTrayIcon();
            _timerEnabled = false;
           
            _tickEnabled = true;
            StartStopTickGenerators();
            RemoveBackgroundWindows();
        }

        private void CreateTrayIcon()
        {
            _trayIcon = new WinForms.NotifyIcon();
            _trayIcon.MouseDoubleClick += NotifyIcon_DoubleClick;
            _trayIcon.Icon = Resources.Tree;
            _trayIcon.Text = "Christmas Lights";
            _trayIcon.ContextMenuStrip = CreateContextMenu();
            _trayIcon.Visible = true;
        }

        private void DestroyTrayIcon()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }

        private WinForms.ContextMenuStrip CreateContextMenu()
        {
            var menu = new WinForms.ContextMenuStrip();
            var mnuSeparator = new WinForms.ToolStripSeparator();
            var mnuSettings = new WinForms.ToolStripMenuItem("Settings...");
            var mnuAbout = new WinForms.ToolStripMenuItem("About...");
            var mnuExit = new WinForms.ToolStripMenuItem("Exit");
            mnuExit.Click += Exit_Click;
            mnuAbout.Click += About_Click;
            mnuSettings.Click += Settings_Click;
            menu.Items.Add(mnuSettings);
            menu.Items.Add(mnuAbout);
            menu.Items.Add(mnuSeparator);
            menu.Items.Add(mnuExit);
            return menu;
        }

        private void NotifyIcon_DoubleClick(object sender, WinForms.MouseEventArgs e)
        {
            if (e.Button == WinForms.MouseButtons.Left)
            {
                Settings_Click(null, null);
            }
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            RemoveBackgroundWindows();
            if (Application.Current.MainWindow != null) Application.Current.MainWindow.Close();
            Application.Current.Shutdown();
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            CreateSettingsWindow();
        }

        private void About_Click(object sender, EventArgs e)
        {
            var about = new AboutWindow();
            about.Owner = Application.Current.MainWindow;
            about.Show();
        }

        private void StartStopTimer()
        {
            if (_timerEnabled)
            {
                if (_timer == null)
                {
                    _timer = new DispatcherTimer();
                    _timerInterval = new TimeSpan(0, 0, 0, 0, Settings.Default.Speed);
                    _timer.Interval = _timerInterval;
                    _timer.Tick += (sender, args) =>
                        Dispatcher.CurrentDispatcher.BeginInvoke(new Action(Tick), DispatcherPriority.SystemIdle);
                }
                _timer.Start();
                _timer.IsEnabled = true;
            }
            else
            {
                if (_timer != null)
                {
                    _timer.IsEnabled = false;
                    _timer.Stop();
                    _timer = null;
                }
            }
        }

        void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            PopulateWindows(true);
        }

        void SystemEvents_DisplaySettingsChanging(object sender, EventArgs e)
        {
            RemoveBackgroundWindows();
        }
        
        void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case (SessionSwitchReason.SessionUnlock): 
                    {
                        _tickEnabled = true;
                        break;
                    }
                case (SessionSwitchReason.SessionLock):
                    {
                        _tickEnabled = false;
                        break;
                    }
            }
            
        }


        void Default_SettingsLoaded(object sender, System.Configuration.SettingsLoadedEventArgs e)
        {
            LoadSettings();
            StartStopTickGenerators();
        }

        private void Default_SettingChanging(object sender, System.Configuration.SettingChangingEventArgs e)
        {
            _tickEnabled = false;
            switch (e.SettingName)
            {
                case "TimerEnabled":
                    _timerEnabled = (bool)e.NewValue;
                    StartStopTimer();
                    break;
                case "BlinkPattern":
                    AllLightsOff();
                    break;
                case "Speed":   
                    _timerInterval =  new TimeSpan(0, 0, 0, 0, (int)e.NewValue);
                    if (_timer != null)
                    {
                        _timer.Interval = _timerInterval;
                    }
                    break;
                case "LightSpacing":
                    _lightSpacing = (int)e.NewValue;
                    PopulateWindows(false);
                    break;
                case "LightHueShift":
                    _lightHueShift = (double)e.NewValue;
                    foreach (var w in _windows)
                    {
                        foreach (var l in w.Lights)
                        {
                            if (l is ShaderLight sl)
                                sl.HueShift = _lightHueShift;
                        }
                    }
                    break;
            }
            _tickEnabled = true;
        }

        private void PopulateWindows(bool recreateBackgroundWindows)
        {
            lock (_windows)
            {
                if (recreateBackgroundWindows)
                {
                    RemoveBackgroundWindows();
                    AddChristmasLightsWindows();
                }

                foreach (var w in _windows)
                {
                    AddLights(w);
                }
                _lastShuffle = DateTime.Now;
            }
        }

        private void RemoveBackgroundWindows()
        {
            lock (_windows)
            {
                foreach (var m in _windows)
                {
                    m.Close();
                }
            }
        }
            
        private void CreateSettingsWindow()
        {
            var settingsWindow = new SettingsWindow(this);
            settingsWindow.Owner = Application.Current.MainWindow;
            settingsWindow.Show();
            settingsWindow.Activate();
        }

        private void AddChristmasLightsWindows()
        {
            var screenNumber = 0;

            foreach (var s in WinForms.Screen.AllScreens)
            {
                var m = new BackWindow(this)
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
                    Name = "ChristmasLights_Window" + screenNumber++.ToString(),
                    Screen = s,
                    Owner = Application.Current.MainWindow 
                };
                _windows.Add(m);
                m.Show();
            }
            _lastShuffle = DateTime.Now;
        }

        private void AddLights(BackWindow m)
        {
            m.Lights.Clear();
            
            m.lightsCanvas.Children.Clear();
            ILight l;

            var leftSide = new List<ILight>();
            var rightSide = new List<ILight>();
            var topLeftSide = new List<ILight>();
            var topRightSide = new List<ILight>();

            var lightSpacing = _lightSpacing;
            var offset = 0;

            if (Settings.Default.BurninPrevention)
            {
                offset = _rnd.Next(-1 * (lightSpacing / 4), (_lightSpacing / 4));
            }

            for (var x = (lightSpacing / 2); x < (m.Screen.WorkingArea.Height - (lightSpacing / 2)); x += lightSpacing)
            {
                l = CreateLight();
                l.Rotate(90 + RandomAngle());
                Canvas.SetLeft((UIElement)l, 5);
                Canvas.SetTop((UIElement)l, (-1 * offset) + x);

                leftSide.Insert(0, l);
                m.lightsCanvas.Children.Add((UIElement)l);

                l = CreateLight();
                l.Rotate(-90 + RandomAngle());
                Canvas.SetRight((UIElement)l, 5);
                Canvas.SetTop((UIElement)l, offset + x);
                
                rightSide.Add(l);
                m.lightsCanvas.Children.Add((UIElement)l);
            }
            
            for (var y = (lightSpacing / 2) + (m.Screen.WorkingArea.Width / 2); y < (m.Screen.WorkingArea.Width - (lightSpacing / 2)); y += lightSpacing)
            {
                l = CreateLight();
                l.Rotate(180 + RandomAngle());
                Canvas.SetTop((UIElement)l, -5);
                Canvas.SetLeft((UIElement)l, offset + y);
                
                topRightSide.Add(l);
                m.lightsCanvas.Children.Add((UIElement)l);

                l = CreateLight();
                l.Rotate(180 + RandomAngle());
                Canvas.SetTop((UIElement)l, -5);
                Canvas.SetRight((UIElement)l, (-1 * offset) + y);
                
                topLeftSide.Insert(0, l);
                m.lightsCanvas.Children.Add((UIElement)l);
            }

            m.Lights.AddRange(leftSide);
            m.Lights.AddRange(topLeftSide);
            m.Lights.AddRange(topRightSide);
            m.Lights.AddRange(rightSide);
            m.LightsCount = m.Lights.Count();
            m.CurrentLight = 0;
        }

        private ILight CreateLight()
        {
            var l = new ShaderLight();
            l.HueShift = _lightHueShift;
            RenderOptions.SetBitmapScalingMode(l, BitmapScalingMode.LowQuality);
            RenderOptions.SetEdgeMode(l, EdgeMode.Aliased);
            RenderOptions.SetCachingHint(l, CachingHint.Cache);
            return l;
        }

        private void SetupFirstRun()
        {
            Settings.Default.BlinkPattern = BlinkPattern.Blink;
            Settings.Default.Speed = 1000;
            Settings.Default.FirstRun = false;
            Settings.Default.BurninPrevention = false;
            Settings.Default.TimerEnabled = true;
            Settings.Default.Save();
            Process.Start("http://www.brokenwire.net/ChristmasLights/thankyou.htm");
            CreateSettingsWindow();
        }

        private void Tick()
        {
            Debug.Write("Tick-");
            Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
            if (_tickEnabled)
            {
                if (_dispatcher.Thread != Thread.CurrentThread)
                {
                    _dispatcher.BeginInvoke(new Action(Tick), DispatcherPriority.SystemIdle, null);
                    return;
                }
                lock (_windows)
                {
                    if ((Settings.Default.BurninPrevention) && _lastShuffle.AddMinutes(5) <= DateTime.Now)
                    {
                        PopulateWindows(false);
                    }

                    switch (Settings.Default.BlinkPattern)
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
                        case BlinkPattern.KnightRider:
                            KnightRider();
                            break;
                        default:
                            AllOnOff();
                            break;
                    }
                }
            }
        }

        private void AllOnOff()
        {
            foreach (var w in _windows)
            {
                foreach (var l in w.Lights)
                {
                    l.Switch();
                }
            }
        }

        private void Interlaced()
        {
            foreach (var w in _windows)
            {
                var skip = w.Lights[0].IsOn();
                foreach (var l in w.Lights)
                {
                    skip = !skip;
                    if (skip)
                    {
                        l.On();
                    }
                    else
                    {
                        l.Off();
                    }
                }
            }
        }

        private int FindLight(BackWindow w, int offset, int step)
        {
            var found = (((w.CurrentLight + offset) + step) + w.LightsCount) % w.LightsCount;
            return found;
        }

        private int PreviousLight(BackWindow w)
        {
            return FindLight(w, 0, -1);
        }

        private int NextLight(BackWindow w)
        {
            return FindLight(w, 0, 1);
        }

        private void Running()
        {
            foreach (var w in _windows)
            {
                w.Lights[FindLight(w, (w.LightsCount / 3), -1)].Off();
                w.Lights[FindLight(w, (w.LightsCount / 3), 1)].On();

                w.Lights[FindLight(w, (w.LightsCount / 3)*2, -1)].Off();
                w.Lights[FindLight(w, (w.LightsCount / 3)*2, 1)].On();


                w.Lights[PreviousLight(w)].Off();
                w.Lights[NextLight(w)].On();
                w.CurrentLight = NextLight(w);
            }
        }

        private void KnightRider()
        {
            foreach (var w in _windows)
            {
                var offset = w.LightsCount - w.CurrentLight;
                var width = w.LightsCount / 4;
                w.Lights[FindLight(w, offset, offset)].Off();
                w.Lights[FindLight(w, 0, -1)].Off();
                w.Lights[FindLight(w, offset, offset -  (2+width))].On();
                w.Lights[FindLight(w, 0, 1 + width)].On();
                
                w.CurrentLight = NextLight(w);
            }
        }

        private void Random()
        {
            foreach (var w in _windows)
            {
                for (var repeat = 0; repeat < (w.LightsCount / 5); repeat++)
                {
                    w.Lights[_rnd.Next(w.LightsCount)].Switch();
                }
            }
        }
        
        private void AllLightsOff()
        {
            foreach (var w in _windows)
            {
                foreach (var l in w.Lights)
                {
                    l.Off();
                }
            }
        }

        private int RandomAngle()
        {
            return _rnd.Next(-20, 20);
        }

        private void KeyHit()
        {
            Tick();
        }

        private DependencyObject FindObject<T>(DependencyObject obj)
        {
            if (obj is T)
            {
                return obj;
            }
            else
            {
                var parent = VisualTreeHelper.GetParent(obj);
                if (parent != null)
                    return FindObject<T>(parent);
                return null;
            }
        }

        private void MouseClick(Point point)
        {
            Debug.Write("Click-");
            Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
            foreach (var window in _windows)
            {
                try
                {
                    var result = VisualTreeHelper.HitTest(window, window.PointFromScreen(point));
                    if (result != null && result.VisualHit != null && result.VisualHit is Image)
                    {
                        var lampje = (ILight) FindObject<ILight>(result.VisualHit);
                        lampje?.Click();
                    }
                }
                catch (Exception e)
                {
                    App.ReportException(e);
                }
            }
        }
    }
}
