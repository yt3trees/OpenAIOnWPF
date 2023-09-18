using System;
using System.Windows;
using System.Windows.Input;

namespace OpenAIOnWPF
{
    /// <summary>
    /// VersionWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class VersionWindow
    {
        public VersionWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            try
            {
                VersionText.Text = "Version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
        private void VersionText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            string exeConfigurationPath = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            Clipboard.SetText(exeConfigurationPath);
            ModernWpf.MessageBox.Show("Configuration file path copied to clipboard.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void githubURLText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var result = ModernWpf.MessageBox.Show("Open the URL in the browser?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(githubURLText.Text) { UseShellExecute = true });
            }
        }
    }
}
