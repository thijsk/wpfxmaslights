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
            var wih = new WindowInteropHelper(this);
            HwndSource source = HwndSource.FromHwnd(wih.Handle);
            source.CompositionTarget.BackgroundColor = Colors.Transparent;
            SetWindowStyle(wih.Handle, WS_EX_NOACTIVATE | WS_EX_TRANSPARENT);
        }

        private void SetWindowStyle(IntPtr hwnd, int flags)
        {
            // Change the extended window style to include WS_EX_TRANSPARENT
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | flags);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
            this.Topmost = true;
        }

    }

    
}
