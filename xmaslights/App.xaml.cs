using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace xmaslights
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private static readonly InterceptKeys.LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private delegate void KeyHit();
        private static KeyHit _keyHit;

        public App()
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            try
            {
                _hookID = InterceptKeys.SetHook(_proc);
            }
            catch
            {
                if (_hookID != IntPtr.Zero)
                    InterceptKeys.UnhookWindowsHookEx(_hookID);
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Controller c = new Controller();
            _keyHit = c.KeyHit;
            c.Launch();
        }

        private static short _keyCount;
        public static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (xmaslights.Properties.Settings.Default.BlinkAsYouType && _keyHit != null && ++_keyCount == 10)
            {
                _keyCount = 0;
                Dispatcher.CurrentDispatcher.BeginInvoke(_keyHit, DispatcherPriority.Input);
            }
            return InterceptKeys.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

    }

}
