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
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Threading;
using xmaslights.Properties;

namespace xmaslights
{
    public class Controller
    {
        private readonly List<BackWindow> _windows = new List<BackWindow>();
        private int _lightSpacing;
        private Random _rnd;
        private DispatcherTimer _timer;
        private DateTime _lastShuffle;
        private TimeSpan _timerInterval;
        private Hooks _hooks;
        private Dispatcher _dispatcher;
        private WinForms.NotifyIcon _trayIcon;
        private BeatDetector _beatDetector;

        private bool _timerEnabled;
        private bool _hooksEnabled;
        private bool _beatDetectEnabled;

        private bool _tickEnabled;

        public Controller()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _rnd = new Random();
           
            Settings.Default.SettingChanging += new System.Configuration.SettingChangingEventHandler(Default_SettingChanging);
            Settings.Default.SettingsLoaded += new System.Configuration.SettingsLoadedEventHandler(Default_SettingsLoaded);
            SystemEvents.DisplaySettingsChanged += new EventHandler(SystemEvents_DisplaySettingsChanged);
            SystemEvents.DisplaySettingsChanging += new EventHandler(SystemEvents_DisplaySettingsChanging);
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
        }

        ~Controller()
        {
            
            SystemEvents.DisplaySettingsChanged -= new EventHandler(SystemEvents_DisplaySettingsChanged);
            SystemEvents.DisplaySettingsChanging -= new EventHandler(SystemEvents_DisplaySettingsChanging);
            SystemEvents.SessionSwitch -= new SessionSwitchEventHandler(SystemEvents_SessionSwitch);

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
            StartStopBeatDetect();
            StartStopHooks();
        }

        private void LoadSettings()
        {
            _lightSpacing = Settings.Default.LightSpacing;
            _timerEnabled = Settings.Default.TimerEnabled;
            _hooksEnabled = Settings.Default.BlinkAsYouType;
            _beatDetectEnabled = Settings.Default.BlinkOnBeat;
        }

        public void Stop()
        {
            DestroyTrayIcon();
            _timerEnabled = false;
            _hooksEnabled = false;
            _beatDetectEnabled = false;

            _tickEnabled = true;
            StartStopTickGenerators();
            this.RemoveBackgroundWindows();
        }

        private void CreateTrayIcon()
        {
            _trayIcon = new WinForms.NotifyIcon();
            _trayIcon.MouseDoubleClick += new WinForms.MouseEventHandler(this.NotifyIcon_DoubleClick);
            _trayIcon.Icon = Properties.Resources.Tree;
            _trayIcon.Text = "Christmas Lights";
            _trayIcon.ContextMenuStrip = this.CreateContextMenu();
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
            WinForms.ContextMenuStrip menu = new WinForms.ContextMenuStrip();
            WinForms.ToolStripSeparator mnuSeparator = new WinForms.ToolStripSeparator();
            WinForms.ToolStripMenuItem mnuSettings = new WinForms.ToolStripMenuItem("Settings...");
            WinForms.ToolStripMenuItem mnuAbout = new WinForms.ToolStripMenuItem("About...");
            WinForms.ToolStripMenuItem mnuExit = new WinForms.ToolStripMenuItem("Exit");
            mnuExit.Click += this.Exit_Click;
            mnuAbout.Click += this.About_Click;
            mnuSettings.Click += this.Settings_Click;
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
                this.Settings_Click(null, null);
            }
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            RemoveBackgroundWindows();
            Application.Current.MainWindow.Close();
            Application.Current.Shutdown();
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            CreateSettingsWindow();
        }

        private void About_Click(object sender, EventArgs e)
        {
            AboutWindow about = new AboutWindow();
            about.Owner = Application.Current.MainWindow;
            about.ShowDialog();
        }

        private void StartStopBeatDetect()
        {
            if (_beatDetectEnabled)
            {
                if (_beatDetector == null)
                {
                    _beatDetector = new BeatDetector();
                    _beatDetector.OnBeat += new Action(delegate { _dispatcher.BeginInvoke(new Action(_beatDetector_OnBeat), DispatcherPriority.SystemIdle, null); });
                }
                _beatDetector.Start();
            }
            else
            {
                if (_beatDetector != null)
                {
                    _beatDetector.Stop();
                    _beatDetector = null;
                }
            }
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
                    _timer.Tick += new EventHandler(delegate(object o, EventArgs e) { Dispatcher.CurrentDispatcher.BeginInvoke(new Action(Tick), DispatcherPriority.SystemIdle); });
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

        private void StartStopHooks()
        {
            bool mouseenabled = true;
            bool keyenabled = this._hooksEnabled;

            if (_hooks == null && (mouseenabled || keyenabled))
            {
                _hooks = new Hooks();
                _hooks.OnKeyUp += new Hooks.KeyUpEvent(delegate { _dispatcher.BeginInvoke(new Action(KeyHit), DispatcherPriority.SystemIdle, null); });
                _hooks.OnMouseUp += new Hooks.MouseUpEvent(delegate(Point pt) { _dispatcher.BeginInvoke(new Action<Point>(MouseClick), DispatcherPriority.SystemIdle, pt); });
            }

            if (keyenabled)
            {
                _hooks.SetBackgroundGlobalLLKeyboardHook();
            }
            else
            {
                _hooks.RemoveBackgroundGlobalLLKeyboardHook();
            }

            if (mouseenabled)
            {
                _hooks.SetBackgroundGlobalLLMouseHook();
            }
            else
            {
                _hooks.RemoveBackgroundGlobalLLMouseHook();
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
                case "BlinkAsYouType":
                    _hooksEnabled = (bool)e.NewValue;
                    StartStopHooks();
                    break;
                case "BlinkOnBeat":
                    _beatDetectEnabled = (bool)e.NewValue;
                    StartStopBeatDetect();
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

                foreach (BackWindow w in _windows)
                {
                    AddLights(w);
                }
                ReduceWorkingSet();
                _lastShuffle = DateTime.Now;
            }
        }

        private void RemoveBackgroundWindows()
        {
            foreach (BackWindow m in _windows)
            {
                m.Close();
            }
        }
            
        private void CreateSettingsWindow()
        {
            SettingsWindow settingsWindow = new SettingsWindow(this);
            settingsWindow.Owner = Application.Current.MainWindow;
            settingsWindow.Show();
            settingsWindow.Activate();
        }

        private void AddChristmasLightsWindows()
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

            List<ILight> leftSide = new List<ILight>();
            List<ILight> rightSide = new List<ILight>();
            List<ILight> topLeftSide = new List<ILight>();
            List<ILight> topRightSide = new List<ILight>();

            int lightSpacing = _lightSpacing;
            int offset = 0;

            if (Properties.Settings.Default.BurninPrevention)
            {
                offset = _rnd.Next(-1 * (lightSpacing / 4), (_lightSpacing / 4));
            }

            for (int x = (lightSpacing / 2); x < (m.Screen.WorkingArea.Height - (lightSpacing / 2)); x += lightSpacing)
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
            
            for (int y = (lightSpacing / 2) + (m.Screen.WorkingArea.Width / 2); y < (m.Screen.WorkingArea.Width - (lightSpacing / 2)); y += lightSpacing)
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
            BitmapLight l = new BitmapLight();
            RenderOptions.SetBitmapScalingMode(l, BitmapScalingMode.LowQuality);
            RenderOptions.SetEdgeMode(l, EdgeMode.Aliased);
            return l;
        }

        private void SetupFirstRun()
        {
            Properties.Settings.Default.BlinkPattern = BlinkPattern.Blink;
            Properties.Settings.Default.Speed = 1000;
            Properties.Settings.Default.FirstRun = false;
            Properties.Settings.Default.BurninPrevention = false;
            Properties.Settings.Default.TimerEnabled = true;
            Properties.Settings.Default.BlinkOnBeat = false;
            Properties.Settings.Default.BlinkAsYouType = false;
            Properties.Settings.Default.Save();
            System.Diagnostics.Process.Start("http://www.brokenwire.net/ChristmasLights/thankyou.htm");
            CreateSettingsWindow();
        }

        private void Tick()
        {
            if (_tickEnabled)
            {
                if (this._dispatcher.Thread != Thread.CurrentThread)
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

        #region Blinking Patterns

        private void AllOnOff()
        {
            foreach (BackWindow w in this._windows)
            {
                foreach (ILight l in w.Lights)
                {
                    l.Switch();
                }
            }
        }

        private void Interlaced()
        {
            foreach (BackWindow w in this._windows)
            {
                bool skip = w.Lights[0].IsOn();
                foreach (ILight l in w.Lights)
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
            int found = (((w.CurrentLight + offset) + step) + w.LightsCount) % w.LightsCount;
            Debug.WriteLine(found);
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
            foreach (BackWindow w in this._windows)
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
            foreach (BackWindow w in this._windows)
            {
                int offset = w.LightsCount - w.CurrentLight;
                int width = w.LightsCount / 4;
                w.Lights[FindLight(w, offset, offset)].Off();
                w.Lights[FindLight(w, 0, -1)].Off();
                w.Lights[FindLight(w, offset, offset -  (2+width))].On();
                w.Lights[FindLight(w, 0, 1 + width)].On();
                
                w.CurrentLight = NextLight(w);
            }
        }

        private void Random()
        {
            foreach (BackWindow w in this._windows)
            {
                for (int repeat = 0; repeat < (w.LightsCount / 10); repeat++)
                {
                    w.Lights[_rnd.Next(w.LightsCount - 1)].Switch();
                }
            }
        }
        
        private void AllLightsOff()
        {
            foreach (BackWindow w in this._windows)
            {
                foreach (ILight l in w.Lights)
                {
                    l.Off();
                }
            }
        }

        #endregion

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
                else
                    return null;
            }
        }
     
        private void MouseClick(Point point)
        {
            Debug.Write("Click");
            Debug.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId);
            foreach (var window in this._windows)
            {
                try
                {
                    var result = VisualTreeHelper.HitTest(window, window.PointFromScreen(point));
                    if (result != null && result.VisualHit != null && result.VisualHit is Image)
                    {
                        var lampje = (ILight)FindObject<ILight>(result.VisualHit);
                        if (lampje != null)
                        {
                            lampje.Click();
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
        }


        private void _beatDetector_OnBeat()
        {
            Debug.Write("Beat");
            Debug.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId);
            Tick();
        }

        private static void ReduceWorkingSet()
        {
            // To keep ppl from getting shocked when looking in the task manager.
            using (Process process = Process.GetCurrentProcess())
            {
                NativeMethod.SetProcessWorkingSetSize(process.Handle, -1, -1);
            }
        }

        private float CheckPowerStatus()
        {
            return System.Windows.Forms.SystemInformation.PowerStatus.BatteryLifePercent;
        }

    }
}
