using OpenAIOnWPF.Model;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenAIOnWPF
{
    /// <summary>
    /// TitleGenerationSettings.xaml の相互作用ロジック
    /// </summary>
    public partial class TitleGenerationSettings
    {
        public TitleGenerationSettings()
        {
            InitializeComponent();

            var cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
            foreach (var culture in cultures)
            {
                // 英語のカルチャー情報を取得して変更
                var englishCulture = new CultureInfo("en-US");
                Thread.CurrentThread.CurrentCulture = englishCulture;
                Thread.CurrentThread.CurrentUICulture = englishCulture;
                // 英語のDisplayNameを取得
                var englishDisplayName = culture.DisplayName;
                // カルチャー情報を元に戻す
                Thread.CurrentThread.CurrentCulture = CultureInfo.CurrentCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.CurrentUICulture;

                LanguageComboBox.Items.Add(englishDisplayName);
            }

            ModelComboBox.ItemsSource = AppSettings.ConfigDataTable.AsEnumerable().Select(x => x.Field<string>("ConfigurationName")).ToList();

            EnableToggleSwitch.IsOn = AppSettings.UseTitleGenerationSetting;
            ModelComboBox.Text = AppSettings.ModelForTitleGenerationSetting;
            LanguageComboBox.Text = AppSettings.TitleLanguageSetting;
            PromptTextBox.Text = AppSettings.TitleGenerationPromptSetting;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!PromptTextBox.Text.Contains("{Prompt}"))
            {
                ModernWpf.MessageBox.Show("PromptTextBox must include \"{Prompt}\".");
                return; // 処理を中断
            }

            AppSettings.UseTitleGenerationSetting = EnableToggleSwitch.IsOn;
            AppSettings.ModelForTitleGenerationSetting = ModelComboBox.Text;
            AppSettings.TitleLanguageSetting = LanguageComboBox.Text;
            AppSettings.TitleGenerationPromptSetting = PromptTextBox.Text;
            DialogResult = true;
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
            }
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                OkButton_Click(sender, e);
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DialogResult = DialogResult == true;
        }
        private void PromptTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                var textBox = sender as TextBox;

                if (e.Delta > 0 && textBox.FontSize < 40)
                {
                    textBox.FontSize += 2;
                }
                else if (e.Delta < 0 && textBox.FontSize > 10)
                {
                    textBox.FontSize -= 2;
                }
                e.Handled = true;
            }
        }
    }
}
