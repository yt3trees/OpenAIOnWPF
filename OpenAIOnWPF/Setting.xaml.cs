using OpenAI.GPT3.ObjectModels.ResponseModels.ModelResponseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OpenAIOnWPF
{
    /// <summary>
    /// Setting.xaml の相互作用ロジック
    /// </summary>
    public partial class Setting : Window
    {
        private string targetSetting = "";
        public string inputResult { get { return ChangeSettings(); } }
        private bool isPassword = false;
        public Setting(string arg, string setting)
        {
            InitializeComponent();
            targetSetting = arg;

            SettingLabel.Content = targetSetting;

            if (targetSetting == "APIKey")
            {
                isPassword = true;
            }

            if (isPassword)
            {
                SettingTextbox.Visibility = Visibility.Collapsed;
                SettingPasswordbox.Visibility = Visibility.Visible;
                SettingPasswordbox.Password = setting;
                SettingPasswordbox.Focus();
            }
            else
            {
                SettingTextbox.Visibility = Visibility.Visible;
                SettingPasswordbox.Visibility = Visibility.Collapsed;
                SettingTextbox.Text = setting;
                SettingTextbox.Focus();
            }
        }
        private string ChangeSettings()
        {
            return isPassword ? SettingPasswordbox.Password : SettingTextbox.Text;
        }
        
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void SettingTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
            }
        }
        private void SettingPasswordbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
            }
        }
        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                DialogResult = true;
            }
        }

    }
}
