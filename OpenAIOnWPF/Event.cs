using OpenAI.GPT3.Tokenizer.GPT3;
using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace OpenAIOnWPF
{
    public partial class MainWindow
    {
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
            if (e.Key == Key.F1)
            {
                string content = "Ctrl + Enter -> Send Message\r\n"
                                + "F2 -> Set Instruction List\r\n"
                                + "F3 -> Set Temperature\r\n"
                                + "F4 -> Set Conversation History Count\r\n"
                                + "F5 -> Set ModelList\r\n"
                                + "F11 -> View conversation history\r\n"
                                + "F12 -> Set API key\r\n";
                ShowMessagebox("Help",content);
            }
            if (e.Key == Key.F2)
            {
                InstructionSettingWindowOpen();
            }
            if (e.Key == Key.F3)
            {
                TemperatureSettingWindowOpen();
            }
            if (e.Key == Key.F4)
            {
                ConversationHistoryCountSettingWindowOpen();
            }
            if (e.Key == Key.F5)
            {
                ModelListSettingWindowOpen();
            }
            if (e.Key == Key.F11)
            {
                ShowTable();
            }
            if (e.Key == Key.F12)
            {
                APIKeySettingWindowOpen();
            }
        }
        private void UserTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            //ctrl+enterで送信
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                _ = ProcessOpenAIAsync();
            }
        }
        private void ExecButton_Click(object sender, RoutedEventArgs e)
        {
            _ = ProcessOpenAIAsync();
        }
        private void UserTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // トークン量を表示
            var tokens = TokenizerGpt3.Encode(UserTextBox.Text);
            string tooltip = $"Tokens : {tokens.Count()}";
            UserTextBox.ToolTip = tooltip; 
        }
        private void NoticeCheckbox_Click(object sender, RoutedEventArgs e)
        {
            noticeFlgSetting = (bool)NoticeCheckbox.IsChecked;
        }

        private void TokensLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShowMessagebox("Tokens",TokensLabel.ToolTip.ToString());
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Model = modelSetting;
            string list = "";
            foreach (var item in modelListSetting)
            {
                list += item + (item == modelListSetting.Last() ? "" : ",");
            }
            Properties.Settings.Default.ModelList = list;
            Properties.Settings.Default.ConversationHistoryCount = conversationHistoryCountSetting;
            Properties.Settings.Default.Temperature = temperatureSetting;
            Properties.Settings.Default.APIKey = apiKeySetting;
            Properties.Settings.Default.NoticeFlg = noticeFlgSetting;
            Properties.Settings.Default.Instruction = instructionSetting;
            Properties.Settings.Default.InstructionList = SerializeArray(instructionListSetting);
            Properties.Settings.Default.Save();
        }
        private void ModelComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            modelSetting = ModelComboBox.SelectedItem.ToString();
        }
        private void InstructionComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (InstructionComboBox.SelectedItem == null) return;
            instructionSetting = InstructionComboBox.SelectedItem.ToString();
        }
        private void AssistantMarkdownText_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0)
                {
                    AssistantMarkdownText.FontSize += 1;
                }
                else
                {
                    AssistantMarkdownText.FontSize -= 1;
                }
            }
        }
        private void UserTextBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0)
                {
                    UserTextBox.FontSize += 1;
                }
                else
                {
                    UserTextBox.FontSize -= 1;
                }
            }
        }
        private void ConversationHistoryButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShowTable();
        }
        private void InstructionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            InstructionSettingWindowOpen();
        }
        private void TemperatureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            TemperatureSettingWindowOpen();
        }
        private void ConversationHistoryCountMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ConversationHistoryCountSettingWindowOpen();
        }
        private void ModelListMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ModelListSettingWindowOpen();
        }
        private void APIKeyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            APIKeySettingWindowOpen();
        }
    }
}
