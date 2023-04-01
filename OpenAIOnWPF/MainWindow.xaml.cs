using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Toolkit.Uwp.Notifications;
using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace OpenAIOnWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Settings.settings
        public string modelSetting =  Properties.Settings.Default.Model;
        public List<string> modelListSetting = Properties.Settings.Default.ModelList.Split(',').ToList();
        public string premiseSetting = Properties.Settings.Default.Premise;
        public string apiKeySetting = Properties.Settings.Default.APIKey;
        public int conversationHistoryCountSetting = Properties.Settings.Default.ConversationHistoryCount;
        public float temperatureSetting = Properties.Settings.Default.Temperature;
        public bool noticeFlgSetting = Properties.Settings.Default.NoticeFlg;

        private List<ChatMessage> conversationHistory = new List<ChatMessage>();
        //private string logSummary = "";
        //private string latestSummary = "";

        public MainWindow()
        {
            InitializeComponent();
            UserTextBox.Focus();
            UserTextBox.MaxHeight = SystemParameters.PrimaryScreenHeight / 2;
            ModelComboBox.ItemsSource = modelListSetting;
            ModelComboBox.Text = modelSetting;
            NoticeCheckbox.IsChecked = noticeFlgSetting;
        }
        private async Task ProcessOpenAIAsync()
        {
            try
            {
                Debug.Print("===== Start processing =====");
                //AssistantTextBox.Text = "";
                AssistantMarkdownText.Markdown = "";
                ExecButton.IsEnabled = false;
                ExecButton.Content = "Sending...";

                if (apiKeySetting == null)
                {
                    ModernWpf.MessageBox.Show("The environment variable OPENAI_API_KEY is not set.");
                    return;
                }
                var openAiService = new OpenAIService(new OpenAiOptions()
                {
                    ApiKey = apiKeySetting
                });

                // デフォルトモデルを設定
                //openAiService.SetDefaultModelId("gpt-3.5-turbo");
                openAiService.SetDefaultModelId(modelSetting);

                // 今回の送信
                var userMessage = UserTextBox.Text;

                Debug.Print("----- Parameter -----");
                Debug.Print($"Temperature:{temperatureSetting}");
                Debug.Print("----- Contents of this message sent -----");
                Debug.Print(premiseSetting);
                //Debug.Print(logSummary);
                Debug.Print(userMessage);

                List<ChatMessage> messages = new List<ChatMessage>();
                messages.Add(ChatMessage.FromSystem(premiseSetting));
                messages.AddRange(conversationHistory);
                messages.Add(ChatMessage.FromUser(userMessage));

                conversationHistory.Add(ChatMessage.FromUser(userMessage));

                var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest()
                {
                    Messages = messages,
                    Temperature = temperatureSetting 
                    //Messages = new List<ChatMessage>
                    //{
                    //    ChatMessage.FromSystem(premiseSetting),
                    //    ChatMessage.FromSystem(logSummary),
                    //    ChatMessage.FromUser(userMessage)
                    //},
                    //MaxTokens = 50,
                });

                if (completionResult.Successful)
                {
                    var result = completionResult.Choices.First();
                    //AssistantTextBox.Text = completionResult.Choices.First().Message.Content;
                    AssistantMarkdownText.Markdown = completionResult.Choices.First().Message.Content;
                    conversationHistory.Add(ChatMessage.FromAssistant(result.Message.Content));
                    if (noticeFlgSetting)
                    {
                        new ToastContentBuilder()
                            .AddText("️AI responded back.")
                            .Show();
                    }
                }
                else
                {
                    if (completionResult.Error == null)
                    {
                        throw new Exception("Unknown Error");
                    }
                    if (noticeFlgSetting)
                    {
                        new ToastContentBuilder()
                            .AddText("️An error has occurred.")
                            .Show();
                    }
                    ModernWpf.MessageBox.Show($"{completionResult.Error.Code}: {completionResult.Error.Message}");
                }
                /*
                ExecButton.Content = "Summarizing...";

                // 要約処理
                Debug.Print("----- Start summary -----");
                // 要約指示の文章を作成
                var summaryPrompt = "Please summarize the contents of the conversation briefly.\r\n";
                //userかassistantの発言かわかるようにconversationHisotryStringに格納
                string conversationHistoryString = "";
                foreach (var item in conversationHistory)
                {
                    if (item.Role == "user")
                    {
                        conversationHistoryString += "User: " + item.Content + "\r\n";
                    }
                    else if (item.Role == "assistant")
                    {
                        conversationHistoryString += "Assistant: " + item.Content + "\r\n";
                    }
                }
                Debug.Print($"{summaryPrompt}{conversationHistoryString}");
                string instructSummary = $"{summaryPrompt}{conversationHistoryString}";

                // 要約指示
                var summaryResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest()
                {
                    Messages = new List<ChatMessage>
                    {
                        ChatMessage.FromSystem(premiseSetting),
                        ChatMessage.FromSystem(instructSummary)
                    },
                    //Model = Models.ChatGpt3_5Turbo,
                    //MaxTokens = 50,
                });

                // 要約結果
                var summary = summaryResult.Choices.First();
                //conversationHistory.Add(ChatMessage.FromSystem(summary.Message.Content));
                Debug.Print("----- Summary Results -----");
                Debug.Print(summary.Message.Content);
                latestSummary = summary.Message.Content;
                logSummary = "We have had the following exchanges in the past.\r\n";
                logSummary += summary.Message.Content;
                */

                // 閾値超え会話履歴を削除
                if (conversationHistory.Count > conversationHistoryCountSetting)
                {
                    conversationHistory.RemoveRange(0, conversationHistory.Count - conversationHistoryCountSetting);
                    //Debug.Print($"会話履歴が{conversationHistoryCountSetting}を超えたので削除しました。");
                    Debug.Print($"Deleted because conversation history exceeded {conversationHistoryCountSetting} conversations.");
                }

                Debug.Print("----- Conversation History -----");
                foreach (var item in conversationHistory)
                {
                    Debug.Print($"{item.Role}: {item.Content}");
                }
            }
            catch(Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.ToString());
            }
            finally
            {
                ExecButton.IsEnabled = true;
                ExecButton.Content = "Send";
                Debug.Print("===== End of process =====");
            }
        }

        private void ExecButton_Click(object sender, RoutedEventArgs e)
        {
            _ = ProcessOpenAIAsync();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
            if (e.Key == Key.F1)
            {
                string content = "Ctrl + Enter -> Send Message\r\n"
                                + "F2 -> Set Model\r\n"
                                + "F3 -> Set Premise\r\n"
                                + "F4 -> Set Conversation History Count\r\n"
                                //+ "F4 -> View a summary of the conversation\r\n"
                                + "F12 -> Set API key\r\n";
                ShowMessagebox("Help",content);
            }
            // モデルリスト設定画面
            if (e.Key == Key.F2)
            {
                string modelListString = "";
                foreach (var item in modelListSetting)
                {
                    modelListString += item + ",";
                }
                modelListString = modelListString.TrimEnd(',');

                string result = ShowSetting("Model", modelListString, "text");
                if (result != "")
                {
                    modelListSetting = result.Split(',').ToList();
                }
                ModelComboBox.ItemsSource = modelListSetting;
            }
            // 前提条件を表示
            if (e.Key == Key.F3)
            {
                string result = ShowSetting("Premise", premiseSetting, "text");
                if (result != "")
                {
                    premiseSetting = result;
                }
            }
            if (e.Key == Key.F4)
            {
                string result = ShowSetting("Conversation history count", conversationHistoryCountSetting.ToString(), "text");
                if (result != "")
                {
                    conversationHistoryCountSetting = int.Parse(result);
                }
            }
            if (e.Key == Key.F5)
            {
                string result = ShowSetting("Temperature", temperatureSetting.ToString(), "number");
                if (result != "")
                {
                    temperatureSetting = float.Parse(result);
                }
            }
            //// 直前の要約を表示
            //if (e.Key == Key.F4)
            //{
            //    //ModernWpf.MessageBox.Show(latestSummary);
            //    ShowMessagebox("Summary",latestSummary);
            //}
            if (e.Key == Key.F11)
            {
                string content = "";
                foreach (var item in conversationHistory)
                {
                    content += $"{item.Role}: {item.Content}\r\n";
                }
                if (content == "")
                {
                    content = "No conversation history.";
                }
                content = (content == "" ? "No conversation history." : content);
                ShowMessagebox("Conversation History",content);
            }
            if (e.Key == Key.F12)
            {
                string result = ShowSetting("APIKey", apiKeySetting, "password");
                if (result != "")
                {
                    apiKeySetting = result;
                }
            }
        }
        private void ShowMessagebox(string title, string content)
        {
            var window = new Messagebox(title,content);
            window.Owner = this;
            window.ShowDialog();
        }
        private string ShowSetting(string targetSetting, string content, string type)
        {
            var window = new Setting(targetSetting, content, type);
            window.Owner = this;
            bool result = (bool)window.ShowDialog();
            return result ? window.inputResult : "";
        }
        private void UserTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            //ctrl+enterで送信
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                _ = ProcessOpenAIAsync();
            }
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
            Properties.Settings.Default.Premise = premiseSetting;
            Properties.Settings.Default.ConversationHistoryCount = conversationHistoryCountSetting;
            Properties.Settings.Default.Temperature = temperatureSetting;
            Properties.Settings.Default.APIKey = apiKeySetting;
            Properties.Settings.Default.NoticeFlg = noticeFlgSetting;
            Properties.Settings.Default.Save();
        }
        private void ModelComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            modelSetting = ModelComboBox.SelectedItem.ToString();
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
    }
}
