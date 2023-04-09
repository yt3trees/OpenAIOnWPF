﻿using OpenAI.GPT3.Tokenizer.GPT3;
using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
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
                                + "F4 -> View conversation history\r\n"
                                + "F5 -> Set ModelList\r\n"
                                + "F11 -> Set Api key(OpenAI)\r\n"
                                + "F12 -> Set Azure OpenAI Parameter\r\n";
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
                ShowTable();
            }
            if (e.Key == Key.F5)
            {
                ModelListSettingWindowOpen();
            }
            if (e.Key == Key.F6)
            {
                var window = new ColorSettings();
                window.Owner = this;
                window.ShowDialog();
            }
            if (e.Key == Key.F11)
            {
                APIKeySettingWindowOpen();
            }
            if (e.Key == Key.F12)
            {
                var window = new AzureParameterWindow();
                window.Owner = this;
                window.ShowDialog();
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
            Properties.Settings.Default.Provider = providerSetting;
            Properties.Settings.Default.AzureAPIKey = azureApiKeySetting;
            Properties.Settings.Default.AzureBaseDomain = baseDomainSetting;
            Properties.Settings.Default.AzureDeploymentId = deploymentIdSetting;
            Properties.Settings.Default.AzureApiVersion = apiVersionSetting;
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
            // ツールチップに内容を表示
            string selectInstructionContent = "";
            if (!String.IsNullOrEmpty(instructionSetting))
            {
                string[] instructionList = instructionListSetting?.Cast<string>().Where((s, i) => i % 2 == 0).ToArray();
                int index = Array.IndexOf(instructionList, instructionSetting);
                selectInstructionContent = instructionListSetting[index, 1];
            }
            InstructionComboBox.ToolTip = "# " + instructionSetting + "\r\n"
                                            + selectInstructionContent;
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
        private void TokenUsage_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var window = new TokenUsageWindow();
            window.Owner = this;
            window.ShowDialog();
        }
        private void InstructionSettingButton_Click(object sender, RoutedEventArgs e)
        {
            InstructionSettingWindowOpen();
        }
        private void TemperatureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            TemperatureSettingWindowOpen();
        }
        private void ModelListMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ModelListSettingWindowOpen();
        }
        private void APIKeyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            APIKeySettingWindowOpen();
        }
        private void ProviderComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            providerSetting = ProviderComboBox.SelectedItem.ToString();
            if (providerSetting == "Azure")
            {
                ModelComboBox.IsEnabled = false;
            }
            else
            {
                ModelComboBox.IsEnabled = true;
            }
        }
        private void AzureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var window = new AzureParameterWindow();
            window.Owner = this;
            window.ShowDialog();
        }
        private void ColorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var window = new ColorSettings();
            window.Owner = this;
            window.ShowDialog();
        }
        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            //UserTextBox.Width = Math.Max(UserTextBox.ActualWidth + e.HorizontalChange, UserTextBox.MinWidth);
            UserTextBox.Height = Math.Max(UserTextBox.ActualHeight + e.VerticalChange, UserTextBox.MinHeight);
        }
    }
}
