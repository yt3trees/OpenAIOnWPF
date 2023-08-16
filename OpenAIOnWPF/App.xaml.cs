using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace OpenAIOnWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Mutex _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            bool isNewInstance;
            _mutex = new Mutex(true, "OpenAIOnWPFMutex", out isNewInstance);

            if (!isNewInstance)
            {
                ModernWpf.MessageBox.Show("The application is already up and running.");
                _mutex = null;
                Application.Current.Shutdown();
            }

            DispatcherUnhandledException += (s, args) => HandleException(args.Exception);
            TaskScheduler.UnobservedTaskException += (s, args) => HandleException(args.Exception?.InnerException);
            AppDomain.CurrentDomain.UnhandledException += (s, args) => HandleException(args.ExceptionObject as Exception);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _mutex?.ReleaseMutex();
        }
        private void HandleException(Exception e)
        {
            if (e == null) return;

            ModernWpf.MessageBox.Show($"An error has occurred.\n{e}", "Abnormal termination", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(1);
        }
    }
}
