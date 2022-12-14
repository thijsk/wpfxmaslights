using System.Windows;
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
            Visibility = Visibility.Collapsed;
            this.controller = c;
            this.DataContext = controller;
            InitializeComponent();
        }
        
        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();
            this.Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Reload();
            this.Close();
        }
    }
}
