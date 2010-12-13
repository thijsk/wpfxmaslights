using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

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
        }

        ~App()
        {

        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (SingleInstance.IsFirstInstance("10aa2e8e-c2c8-4205-ae55-ac19d925e9eb"))
            {
                CreateMainWindow();
                c = new Controller();
            }
            else
            {
                this.Shutdown();
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
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

                    Exception exception = e.Exception;
                    ExceptionReporting.Core.ExceptionReporter reporter = new ExceptionReporting.Core.ExceptionReporter();
                    reporter.Config.ShowFullDetail = false;
                    reporter.Config.EmailReportAddress = "ChristmasLightsSupport@brokenwire.net";
                    reporter.Config.WebUrl = "http://www.brokenwire.net/";
                    reporter.Show(exception);
                    
                    this.Shutdown(1);
                }
                e.Handled = true;
            }
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
