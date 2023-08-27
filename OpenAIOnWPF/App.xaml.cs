using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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

            // ModernWpf.MessageBox.Show($"An error has occurred.\n{e}", "Abnormal termination", MessageBoxButton.OK, MessageBoxImage.Error);
            ShowCustomErrorDialog("An error has occurred.\n" + e.ToString());
            Environment.Exit(1);
        }
        private void ShowCustomErrorDialog(string message)
        {
            Window errorWindow = new Window
            {
                Title = "Abnormal termination",
                Width = 500,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            TextBox textBox = new TextBox
            {
                Text = message,
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            errorWindow.Content = textBox;
            errorWindow.ShowDialog();
            Environment.Exit(1);
        }
    }
}
