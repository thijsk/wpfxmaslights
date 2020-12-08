using System;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;

namespace xmaslights
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Controller c;

        public App()
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            c = new Controller();
            AppDomain.CurrentDomain.FirstChanceException += new EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs>(CurrentDomain_FirstChanceException);
        }

        ~App()
        {

        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (SingleInstance.IsFirstInstance("10aa2e8e-c2c8-4205-ae55-ac19d925e9eb"))
            {
                CreateMainWindow();
                c.Start();
            }
            else
            {
                this.Shutdown();
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            c.Stop();
            SingleInstance.Close();
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
                    ReportException(e.Exception);
                    e.Handled = true;
                }
                this.Shutdown(1);
            }
        }

        internal static void ReportException(Exception e)
        {
            Debug.Write(e);
        }

        void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            ReportException(e.Exception);
        }

        private void CreateMainWindow()
        {
            Application.Current.MainWindow = new Window()
            {
                Top = -1,
                Left = -1,
                Width = 1,
                Height = 1,
                WindowStyle = WindowStyle.ToolWindow
            };
            Application.Current.MainWindow.ShowInTaskbar = false;
            Application.Current.MainWindow.Show();
            Application.Current.MainWindow.Hide();
        }

    

    }

}
