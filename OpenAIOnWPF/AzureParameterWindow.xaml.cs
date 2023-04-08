using System.Windows;
using static OpenAIOnWPF.MainWindow;

namespace OpenAIOnWPF
{
    /// <summary>
    /// AzureParameterWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AzureParameterWindow : Window
    {
        public AzureParameterWindow()
        {
            InitializeComponent();
            Passwordbox.Password = azureApiKeySetting;
            BaseDomainTextbox.Text = baseDomainSetting;
            ApiVersionTextbox.Text = apiVersionSetting;
            DeploymentIdTextbox.Text = deploymentIdSetting;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            azureApiKeySetting = Passwordbox.Password;
            baseDomainSetting = BaseDomainTextbox.Text;
            apiVersionSetting = ApiVersionTextbox.Text;
            deploymentIdSetting = DeploymentIdTextbox.Text;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
