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
using xmaslights.Properties;

namespace xmaslights
{
    /// <summary>
    /// Interaction logic for TrayWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {

        private readonly Controller controller;
        public Controller Controller { get { return controller; } }

        public SettingsWindow(Controller c)
        {
            this.controller = c;
            this.DataContext = controller;
            InitializeComponent();
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Visible;
        }

        private void OnNotifyIconDoubleClick(object sender, MouseButtonEventArgs e)
        {
           // nothing to do
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();
            this.Visibility = Visibility.Collapsed;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Reload();
            this.Visibility = Visibility.Collapsed;
        }

        private void slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            controller.Timer.Interval = new TimeSpan(0, 0, 0, 0, (int)e.NewValue);
        }

    }
}
