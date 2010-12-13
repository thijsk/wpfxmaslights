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
using BeatDetect;
using System.Threading;

namespace xmaslights
{
    public class Controller
    {
        private readonly List<BackWindow> _windows = new List<BackWindow>();
        private int _lightSpacing;
        private SettingsWindow _settingsWindow;
        private Random _rnd;
        private DispatcherTimer _timer;
        private bool skip = false;
        private DateTime _lastShuffle;
        private TimeSpan _timerInterval;
        private Hooks _hooks;
        private Dispatcher _dispatcher;
        private WinForms.NotifyIcon _trayIcon;
        private BeatDetector _beatDetector;

        public Controller()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            CreateSettingsWindow();
            CreateTrayIcon();
            
            _rnd = new Random();
            _timer = new DispatcherTimer();
            _beatDetector = new BeatDetector();
            _beatDetector.OnBeat += new Action(delegate { _dispatcher.BeginInvoke(new Action(_beatDetector_OnBeat), DispatcherPriority.SystemIdle, null); });
          
            _hooks = new Hooks();
            _hooks.OnKeyUp += new Hooks.KeyUpEvent(delegate { _dispatcher.BeginInvoke(new Action(KeyHit), DispatcherPriority.SystemIdle, null); });
            _hooks.OnMouseUp += new Hooks.MouseUpEvent(delegate(Point pt) { _dispatcher.BeginInvoke(new Action<Point>(MouseClick), DispatcherPriority.SystemIdle, pt); });

            if (Properties.Settings.Default.FirstRun)
                SetupFirstRun();

            Properties.Settings.Default.SettingChanging += new System.Configuration.SettingChangingEventHandler(Default_SettingChanging);
            _lightSpacing = Properties.Settings.Default.LightSpacing;
            
            AddChristmasLightsWindows();
            PopulateWindows(false);
            
            SystemEvents.DisplaySettingsChanged += new EventHandler(SystemEvents_DisplaySettingsChanged);
            SystemEvents.DisplaySettingsChanging += new EventHandler(SystemEvents_DisplaySettingsChanging);
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);


            _beatDetector.Start();
            SetHooks(Properties.Settings.Default.BlinkAsYouType, true);
            StartTimer();
            //_player = new MediaPlayer();
            //_player.Open(new Uri("http://www.sky.fm/wma/christmas.asx"));
            //_player.Play();

            Debug.Write("Start");
            Debug.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId);
        }


        ~Controller()
        {
            _beatDetector.Stop();   
            _hooks.RemoveBackgroundGlobalLLKeyboardHook();
            _hooks.RemoveBackgroundGlobalLLMouseHook();
            SystemEvents.DisplaySettingsChanged -= new EventHandler(SystemEvents_DisplaySettingsChanged);
            SystemEvents.DisplaySettingsChanging -= new EventHandler(SystemEvents_DisplaySettingsChanging);
            SystemEvents.SessionSwitch -= new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
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

        private WinForms.ContextMenuStrip CreateContextMenu()
        {
            WinForms.ContextMenuStrip menu = new WinForms.ContextMenuStrip();
            WinForms.ToolStripSeparator mnuSeparator = new WinForms.ToolStripSeparator();
            WinForms.ToolStripMenuItem mnuSettings = new WinForms.ToolStripMenuItem("Settings...");
            WinForms.ToolStripMenuItem mnuAbout = new WinForms.ToolStripMenuItem("About...");
            WinForms.ToolStripMenuItem mnuExit = new WinForms.ToolStripMenuItem("Exit");
            mnuExit.Font = new System.Drawing.Font(System.Drawing.SystemFonts.MenuFont, System.Drawing.FontStyle.Bold);
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
            _settingsWindow.Show();
            _settingsWindow.Activate();
        }

        private void About_Click(object sender, EventArgs e)
        {
            AboutWindow about = new AboutWindow();
            about.Owner = Application.Current.MainWindow;
            about.ShowDialog();
        }

        private void SetHooks(bool keyenabled, bool mouseenabled)
        {
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
                        _timer.Start();
                        break;
                    }
                case (SessionSwitchReason.SessionLock):
                    {
                        _timer.Stop();
                        break;
                    }
            }
            
        }

        private void Default_SettingChanging(object sender, System.Configuration.SettingChangingEventArgs e)
        {
            switch (e.SettingName)
            {
                case "TimerEnabled":
                    _timer.IsEnabled = (bool)e.NewValue;
                    break;
                case "BlinkPattern":
                    AllLightsOff();
                    break;
                case "Speed":   
                    _timerInterval =  new TimeSpan(0, 0, 0, 0, (int)e.NewValue);
                    _timer.Interval = _timerInterval;
                    break;
                case "LightSpacing":
                    _lightSpacing = (int)e.NewValue;
                    PopulateWindows(false);
                    break;
                case "BlinkAsYouType":
                    SetHooks((bool)e.NewValue, true);
                    break;
            }
        }

        private void PopulateWindows(bool recreateBackgroundWindows)
        {
            lock (_windows)
            {
                _timer.IsEnabled = false;
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
                _timer.IsEnabled = Properties.Settings.Default.TimerEnabled;
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
            _settingsWindow = new SettingsWindow(this);
            _settingsWindow.Owner = Application.Current.MainWindow;
            _settingsWindow.Hide();
        }

        private void StartTimer()
        {
            _timerInterval = new TimeSpan(0, 0, 0, 0, Properties.Settings.Default.Speed);  

            _timer.Interval = _timerInterval;
            _timer.Tick += new EventHandler(delegate(object o, EventArgs e){ Dispatcher.CurrentDispatcher.BeginInvoke(new Action(Tick), DispatcherPriority.SystemIdle); });
            _timer.Start();
            _timer.IsEnabled = Properties.Settings.Default.TimerEnabled;
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
            Properties.Settings.Default.Save();
            System.Diagnostics.Process.Start("http://www.brokenwire.net/ChristmasLights/thankyou.htm");
            _settingsWindow.Visibility = Visibility.Visible;
        }

        private void Tick()
        {
            if (this._dispatcher.Thread != Thread.CurrentThread)
            {
                _dispatcher.BeginInvoke(new Action(Tick), DispatcherPriority.SystemIdle, null);
                return;
            }
            lock (_windows)
            {
                if ((Properties.Settings.Default.BurninPrevention) && _lastShuffle.AddMinutes(5) <= DateTime.Now)
                {
                    PopulateWindows(false);
                }
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
                skip = w.Lights[0].IsOn();
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

        private int PreviousLight(BackWindow w)
        {
            return (w.CurrentLight -1 + w.LightsCount) % w.LightsCount;
        }

        private int NextLight(BackWindow w)
        {
            return (w.CurrentLight + 1) % w.LightsCount;
        }

        private void Running()
        {
            foreach (BackWindow w in this._windows)
            {
                w.Lights[PreviousLight(w)].Off();
                w.Lights[NextLight(w)].On();
                w.CurrentLight = NextLight(w);
            }
        }

        private void Random()
        {
            foreach (BackWindow w in this._windows)
            {
                w.Lights[_rnd.Next(w.LightsCount - 1)].Switch();
            }
        }
        
   

        internal void AllLightsOff()
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

        internal void KeyHit()
        {
            Tick();
        }

        public DependencyObject FindObject<T>(DependencyObject obj)
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
