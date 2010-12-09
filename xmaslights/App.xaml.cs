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
        public App()
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        ~App()
        {

        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Controller c = new Controller();
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
