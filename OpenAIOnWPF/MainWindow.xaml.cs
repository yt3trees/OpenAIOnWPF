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
using OpenAI.GPT3.Tokenizer.GPT3;

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

                var sw = new Stopwatch();
                sw.Start();
                TimeLabel.Content = "";
                TokensLabel.Content = "";
                ProgressBar.Visibility = Visibility.Visible;
                ProgressBar.IsIndeterminate = true;

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
                Debug.Print(userMessage);

                List<ChatMessage> messages = new List<ChatMessage>();
                messages.AddRange(conversationHistory);
                messages.Add(ChatMessage.FromSystem(premiseSetting));
                messages.Add(ChatMessage.FromUser(userMessage));

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
                    AssistantMarkdownText.Markdown = completionResult.Choices.First().Message.Content;

                    // トークン量を計算してツールチップで表示
                    string conversationHistoryString = "";
                    foreach (var item in conversationHistory)
                    {
                        conversationHistoryString += item.Content;
                    }
                    var conversationResultTokens = TokenizerGpt3.Encode(conversationHistoryString);
                    var premiseTokens = TokenizerGpt3.Encode(premiseSetting);
                    var userTokens = TokenizerGpt3.Encode(userMessage);
                    var responseTokens = TokenizerGpt3.Encode(completionResult.Choices.First().Message.Content);
                    var totalTokens = conversationResultTokens.Count() + premiseTokens.Count() + userTokens.Count() + responseTokens.Count();
                    string tooltip = "";
                    tooltip += $"Conversation Result Tokens : {conversationResultTokens.Count()}\r\n";
                    tooltip += $"Premise Tokens : {premiseTokens.Count()}\r\n";
                    tooltip += $"User Messages Tokens : {userTokens.Count()}\r\n";
                    tooltip += $"AI Response Tokens : {responseTokens.Count()}\r\n";
                    tooltip += $"Total Tokens : {totalTokens}";
                    TokensLabel.Content = totalTokens;
                    TokensLabel.ToolTip = tooltip;

                    conversationHistory.Add(ChatMessage.FromUser(userMessage));
                    conversationHistory.Add(ChatMessage.FromAssistant(result.Message.Content));

                    if (noticeFlgSetting)
                    {
                        new ToastContentBuilder()
                            .AddText("️AI responded back.")
                            .Show();
                    }

                    sw.Stop();
                    TimeLabel.Content = $"{sw.ElapsedMilliseconds} ms";
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
                    sw.Stop();
                    ModernWpf.MessageBox.Show($"{completionResult.Error.Code}: {completionResult.Error.Message}");
                }

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
                ProgressBar.Visibility = Visibility.Collapsed;
                ProgressBar.IsIndeterminate = false;
                Debug.Print("===== End of process =====");
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

        private void PremiseSettingWindowOpen()
        {
            string result = ShowSetting("Premise", premiseSetting, "text");
            if (result != "")
            {
                premiseSetting = result;
            }
        }
        private void TemperatureSettingWindowOpen()
        {
            string result = ShowSetting("Temperature", temperatureSetting.ToString(), "number");
            if (result != "")
            {
                temperatureSetting = float.Parse(result);
            }
        }
        private void ConversationHistoryCountSettingWindowOpen()
        {
            string result = ShowSetting("Conversation history count", conversationHistoryCountSetting.ToString(), "text");
            if (result != "")
            {
                conversationHistoryCountSetting = int.Parse(result);
            }
        }
        private void ModelListSettingWindowOpen()
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
        private void APIKeySettingWindowOpen()
        {
            string result = ShowSetting("APIKey", apiKeySetting, "password");
            if (result != "")
            {
                apiKeySetting = result;
            }
        }
    }
}
