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
        string targetSetting = "";
        public Setting(string arg)
        {
            InitializeComponent();
            targetSetting = arg;

            SettingPasswordbox.Visibility = Visibility.Collapsed;
            if (targetSetting == "Model")
            {
                SettingLabel.Content = "Model";
                SettingTextbox.Focus();
                SettingTextbox.Text = Properties.Settings.Default.Model;
            }
            else if (targetSetting == "Premise")
            {
                SettingLabel.Content = "Premise";
                SettingTextbox.Focus();
                SettingTextbox.Text = Properties.Settings.Default.Premise;
            }
            else if (targetSetting == "APIKey")
            {
                SettingLabel.Content = "API Key";
                SettingPasswordbox.Focus();
                SettingTextbox.Visibility = Visibility.Collapsed;
                SettingPasswordbox.Visibility = Visibility.Visible;
                SettingPasswordbox.Password = Properties.Settings.Default.APIKey;
            }
            else if (targetSetting == "Conversation history count")
            {
                SettingLabel.Content = "Conversation History Count";
                SettingTextbox.Focus();
                SettingTextbox.Text = Properties.Settings.Default.ConversationHistoryCount.ToString();
            }
        }
        private void ChangeSettings()
        {
            if (targetSetting == "Model")
            {
                ((MainWindow)this.Owner).modelSetting = SettingTextbox.Text;
                ((MainWindow)this.Owner).Title = "OpenAI " + ((MainWindow)this.Owner).modelSetting;
                this.Close();
            }
            else if (targetSetting == "Premise")
            {
                ((MainWindow)this.Owner).premiseSetting = SettingTextbox.Text;
                this.Close();
            }
            else if (targetSetting == "APIKey")
            {
                ((MainWindow)this.Owner).apiKeySetting = SettingPasswordbox.Password;
                this.Close();
            }
            else if (targetSetting == "Conversation history count")
            {
                ((MainWindow)this.Owner).conversationHistoryCountSetting = int.Parse(SettingTextbox.Text);
                this.Close();
            }
        }
        
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeSettings();
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SettingTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
        private void SettingPasswordbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ChangeSettings();
            }
        }

    }
}
