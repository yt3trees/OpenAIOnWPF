using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace OpenAIOnWPF
{
    /// <summary>
    /// TranslateAPISettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TranslationAPISettingWindow
    {
        MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
        public TranslationAPISettingWindow()
        {
            InitializeComponent();

            TranslationAPIProviderComboBox.Items.Add("DeepL");
            TranslationAPIProviderComboBox.Items.Add("Google");
            TranslationAPIProviderComboBox.SelectedItem = AppSettings.TranslationAPIProvider;

            if(AppSettings.TranslationAPIProvider == "DeepL")
            {
                APIURLTextBox.Text = AppSettings.TranslationAPIUrlDeepL;
                APIKeyPasswordBox.Password = AppSettings.TranslationAPIKeyDeepL;
            }
            else if(AppSettings.TranslationAPIProvider == "Google")
            {
                APIURLTextBox.Text = AppSettings.TranslationAPIUrlGoogle;
                APIURLTextBox.IsEnabled = false;
                APIKeyPasswordBox.Password = AppSettings.TranslationAPIKeyGoogle;
            }
            UseTranslateAPIToggleSwitch.IsOn = AppSettings.TranslationAPIUseFlg;
            PopulateLanguageComboBox();

            FromTranslationLanguageComboBox.SelectedItem = AppSettings.FromTranslationLanguage;
            ToTranslationLanguageComboBox.SelectedItem = AppSettings.ToTranslationLanguage;
        }
        private void PopulateLanguageComboBox()
        {
            var languageCodes = new List<string>
            {
                "BG", "CS", "DA", "DE", "EL", "EN", "ES", "ET", "FI", "FR",
                "HU", "ID", "IT", "JA", "KO", "LT", "LV", "NB", "NL", "PL",
                "PT", "RO", "RU", "SK", "SL", "SV", "TR", "UK", "ZH"
            };

            foreach (var code in languageCodes)
            {
                FromTranslationLanguageComboBox.Items.Add(code);
                ToTranslationLanguageComboBox.Items.Add(code);
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (AppSettings.TranslationAPIUseFlg == false)
            {
                UseTranslateAPIToggleSwitch.IsOn = false;
                mainWindow.TranslateAPIGridColumn.Width = new GridLength(0);
            }
            else
            {
                UseTranslateAPIToggleSwitch.IsOn = true;
                mainWindow.TranslateAPIGridColumn.Width = new GridLength(1, GridUnitType.Auto);
            }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
            }
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (TranslationAPIProviderComboBox.Text == "DeepL")
            {
                AppSettings.TranslationAPIUrlDeepL = APIURLTextBox.Text;
                AppSettings.TranslationAPIKeyDeepL = APIKeyPasswordBox.Password;
            }
            else if (TranslationAPIProviderComboBox.Text == "Google")
            {
                AppSettings.TranslationAPIUrlGoogle = APIURLTextBox.Text;
                AppSettings.TranslationAPIKeyGoogle = APIKeyPasswordBox.Password;
            }
            AppSettings.FromTranslationLanguage = FromTranslationLanguageComboBox.SelectedItem.ToString();
            AppSettings.ToTranslationLanguage = ToTranslationLanguageComboBox.SelectedItem.ToString();
            AppSettings.TranslationAPIUseFlg = UseTranslateAPIToggleSwitch.IsOn;
            AppSettings.TranslationAPIProvider = TranslationAPIProviderComboBox.SelectedItem.ToString();
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
        private void UseTranslateAPIToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (UseTranslateAPIToggleSwitch.IsOn == false)
            {
                mainWindow.TranslateAPIGridColumn.Width = new GridLength(0);
            }
            else
            {
                mainWindow.TranslateAPIGridColumn.Width = new GridLength(1, GridUnitType.Auto);
            }
        }
        private void TranslationAPIProviderComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (TranslationAPIProviderComboBox.SelectedItem.ToString() == "DeepL")
            {
                APIURLTextBox.Text = AppSettings.TranslationAPIUrlDeepL;
                APIURLTextBox.IsEnabled = true;
                APIKeyPasswordBox.Password = AppSettings.TranslationAPIKeyDeepL;
            }
            else if (TranslationAPIProviderComboBox.SelectedItem.ToString() == "Google")
            {
                APIURLTextBox.Text = AppSettings.TranslationAPIUrlGoogle;
                APIURLTextBox.IsEnabled = false;
                APIKeyPasswordBox.Password = AppSettings.TranslationAPIKeyGoogle;
            }
        }
    }
}
