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
using System.Windows.Shapes;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;


namespace xmaslights
{
    /// <summary>
    /// Interaction logic for BackWindow.xaml
    /// </summary>
    public partial class BackWindow : Window
    {
        private readonly Controller controller;
        public Controller Controller { get { return controller; } }

        public BackWindow(Controller c)
        {
            
            InitializeComponent();

            this.controller = c;
            this.DataContext = controller;

            this.MouseUp += new MouseButtonEventHandler(BackWindow_MouseUp);
        }

        void BackWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine(e.GetPosition(this));
            e.Handled = true;
        }

        
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int GWL_EXSTYLE = (-20);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        public System.Windows.Forms.Screen Screen
        {
            get;
            set;
        }

        private readonly List<ILight> _lights = new List<ILight>();
        
        public List<ILight> Lights
        {
            get
            {
                return _lights;
            }
        }

        public int LightsCount
        {
            get;
            set;
        }

        public int CurrentLight
        {
            get;
            set;
        }

        protected override void  OnSourceInitialized(EventArgs e)
        {
 	        base.OnSourceInitialized(e);
            // Get this window's handle
            SetWindowStyle(new WindowInteropHelper(this).Handle, WS_EX_NOACTIVATE | WS_EX_TRANSPARENT);
        }

        private void SetWindowStyle(IntPtr hwnd, int flags)
        {
            // Change the extended window style to include WS_EX_TRANSPARENT
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | flags);
        }

        //private static IntPtr AddWndProc(Window window)
        //{
        //    IntPtr hwnd = new WindowInteropHelper(window).Handle;
        //    HwndSource source = HwndSource.FromHwnd(hwnd);
        //    source.AddHook(new HwndSourceHook(WndProc));
        //    return hwnd;
        //}

        //private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        //{
        //    Debug.WriteLine(msg.ToString());
        //    if (msg == (int)WM.MOUSEMOVE)
        //    {
        //        Debug.WriteLine("MM");

        //    }
        //    handled = false;
        //    return IntPtr.Zero;
        //}

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
        }

    }

    
}
