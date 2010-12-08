using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace xmaslights
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private static readonly GlobalHooks.HookProc _keyProc = KeyHookCallback;
        private static readonly GlobalHooks.HookProc _mouseProc = MouseHookCallback;
        private static IntPtr _keyHookID = IntPtr.Zero;
        private static IntPtr _mouseHookID = IntPtr.Zero;
        private delegate void KeyHit();
        private delegate void MouseClick(Point point);
        private static KeyHit _keyHit;
        private static MouseClick _mouseClick;

        public App()
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            try
            {
                _keyHookID = GlobalHooks.SetKeyHook(_keyProc);
                _mouseHookID = GlobalHooks.SetMouseHook(_mouseProc);
            }
            catch
            {
                if (_keyHookID != IntPtr.Zero)
                    GlobalHooks.UnhookWindowsHookEx(_keyHookID);
                if (_mouseHookID != IntPtr.Zero)
                    GlobalHooks.UnhookWindowsHookEx(_mouseHookID);
            }
        }

        ~App()
        {
             if (_keyHookID != IntPtr.Zero)
                    GlobalHooks.UnhookWindowsHookEx(_keyHookID);
             if (_mouseHookID != IntPtr.Zero)
                   GlobalHooks.UnhookWindowsHookEx(_mouseHookID);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Controller c = new Controller();
            _keyHit = c.KeyHit;
            _mouseClick = c.MouseClick;
        }

        private static short _keyCount;
        private static IntPtr KeyHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (xmaslights.Properties.Settings.Default.BlinkAsYouType && _keyHit != null && ++_keyCount == 5)
            {
                _keyCount = 0;
                Dispatcher.CurrentDispatcher.BeginInvoke(_keyHit, DispatcherPriority.SystemIdle);
            }
            return GlobalHooks.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (wParam == (IntPtr)WM.LBUTTONUP && _mouseClick != null)
            {
                GlobalHooks.MSLLHOOKSTRUCT mousestruct = (GlobalHooks.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(GlobalHooks.MSLLHOOKSTRUCT));
                Dispatcher.CurrentDispatcher.BeginInvoke(_mouseClick, DispatcherPriority.SystemIdle, (Point)mousestruct.pt);
            }
            return GlobalHooks.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        private bool _firstException = true;
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            lock (this)
            {
                if (_firstException)
                {
                    _firstException = false;
                    foreach (Window w in this.Windows)
                    {
                        w.Close();
                    }

                    Exception exception = e.Exception;

                    ExceptionReporting.Core.ExceptionReporter reporter = new ExceptionReporting.Core.ExceptionReporter();

                    string email = "ChristmasLightsSupport@brokenwire.net";

                    reporter.Config.ShowFullDetail = false;

                    reporter.Config.EmailReportAddress = email;
                    reporter.Config.WebUrl = "http://www.brokenwire.net/";

                    reporter.Show(exception);
                    
                    Application.Current.Shutdown(1);
                }
                e.Handled = true;
            }
        }

    }

}
