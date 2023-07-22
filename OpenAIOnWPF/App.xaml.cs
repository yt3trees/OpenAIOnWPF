using System.Threading;
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
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _mutex?.ReleaseMutex();
        }
    }
}
