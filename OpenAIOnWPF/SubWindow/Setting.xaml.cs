using OpenAI.ObjectModels.ResponseModels.ModelResponseModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public partial class Setting
    {
        private string targetSetting = "";
        public string inputResult { get { return ChangeSettings(); } }
        private bool isText = false;
        private bool isPassword = false;
        private bool isNumber = false;
        public Setting(string arg, string setting, string type)
        {
            InitializeComponent();
            targetSetting = arg;

            SettingLabel.Content = targetSetting;

            if (type == "text")
            {
                isText = true;
            }
            else if (type == "password")
            {
                isPassword = true;
            }
            else if (type == "number")
            {
                isNumber = true;
            }
            else if (type == "int")
            {
                isNumber = true;
                SettingNumberbox.SmallChange = 1;
                SettingNumberbox.Maximum = 10000000000000;
            }
            else
            {
                isText = true;
            }

            SettingTextbox.Visibility = Visibility.Collapsed;
            SettingPasswordbox.Visibility = Visibility.Collapsed;
            SettingNumberbox.Visibility = Visibility.Collapsed;

            if (isText)
            {
                SettingTextbox.Visibility = Visibility.Visible;
                SettingTextbox.Text = setting;
                SettingTextbox.Focus();
            }
            else if (isPassword)
            {
                SettingPasswordbox.Visibility = Visibility.Visible;
                SettingPasswordbox.Password = setting;
                SettingPasswordbox.Focus();
            }
            else if (isNumber)
            {
                SettingNumberbox.Visibility = Visibility.Visible;
                SettingNumberbox.Text = setting;
                SettingNumberbox.Focus();
            }
        }
        private string ChangeSettings()
        {
            if (isText)
            {
                return SettingTextbox.Text;
            }
            else if (isPassword)
            {
                return SettingPasswordbox.Password;
            }
            else if (isNumber)
            {
                return SettingNumberbox.Text;
            }
            else
            {
                return SettingTextbox.Text;
            }
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
        private void SettingNumberbox_KeyDown(object sender, KeyEventArgs e)
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
