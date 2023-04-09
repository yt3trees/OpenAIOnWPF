using Microsoft.Toolkit.Uwp.Notifications;
using ModernWpf;
using Newtonsoft.Json;
using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.Tokenizer.GPT3;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace OpenAIOnWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// モデル
        /// </summary>
        public string modelSetting =  Properties.Settings.Default.Model;
        /// <summary>
        /// 指定できるモデルのリスト
        /// </summary>
        public List<string> modelListSetting = Properties.Settings.Default.ModelList.Split(',').ToList();
        /// <summary>
        /// 指示内容
        /// </summary>
        public static string instructionSetting = Properties.Settings.Default.Instruction;
        /// <summary>
        /// 指示内容のリスト
        /// </summary>
        public string[,] instructionListSetting = DeserializeArray(Properties.Settings.Default.InstructionList);
        /// <summary>
        /// APIキー(OpenAi)
        /// </summary>
        public string apiKeySetting = Properties.Settings.Default.APIKey;
        /// <summary>
        /// 会話履歴の保持数
        /// </summary>
        public static int conversationHistoryCountSetting = Properties.Settings.Default.ConversationHistoryCount;
        /// <summary>
        /// Temperatureパラメータ(0~2)
        /// </summary>
        public float temperatureSetting = Properties.Settings.Default.Temperature;
        /// <summary>
        /// 通知フラグ
        /// </summary>
        public bool noticeFlgSetting = Properties.Settings.Default.NoticeFlg;
        /// <summary>
        /// トークン使用量
        /// </summary>
        public string[,] tokenUsageSetting = DeserializeArray(Properties.Settings.Default.TokenUsage);
        /// <summary>
        /// 選択プロバイダ
        /// </summary>
        public static string providerSetting = Properties.Settings.Default.Provider;
        /// <summary>
        /// APIキー(Azure)
        /// </summary>
        public static string azureApiKeySetting = Properties.Settings.Default.AzureAPIKey;
        /// <summary>
        /// Azureエンドポイント
        /// </summary>
        public static string baseDomainSetting = Properties.Settings.Default.AzureBaseDomain;
        /// <summary>
        /// Azureモデルデプロイ名
        /// </summary>
        public static string deploymentIdSetting = Properties.Settings.Default.AzureDeploymentId;
        /// <summary>
        /// Apiバージョン
        /// </summary>
        public static string apiVersionSetting = Properties.Settings.Default.AzureApiVersion;
        /// <summary>
        /// 会話履歴
        /// </summary>
        private List<ChatMessage> conversationHistory = new List<ChatMessage>();
        /// <summary>
        /// DataBinding用クラス
        /// </summary>
        public class DataBind
        {
            public string? PlaceHolder { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            InitialColorSet();
            UserTextBox.Focus();
            UserTextBox.MaxHeight = SystemParameters.PrimaryScreenHeight / 2;
            ModelComboBox.ItemsSource = modelListSetting;
            ModelComboBox.Text = modelSetting;
            NoticeCheckbox.IsChecked = noticeFlgSetting;

            string[] instructionList = instructionListSetting?.Cast<string>().Where((s, i) => i % 2 == 0).ToArray();
            if (instructionList != null)
            {
                Array.Resize(ref instructionList, instructionList.Length + 1);
                instructionList[instructionList.Length - 1] = "";
                InstructionComboBox.ItemsSource =instructionList ;
                InstructionComboBox.Text = String.IsNullOrEmpty(instructionSetting) ? "" : instructionSetting;
            }

            var appSettings = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal);
            Debug.Print("Path to save the configuration file:" + appSettings.FilePath);

            List<string> providers = new List<string>
            {
                "OpenAI",
                "Azure"
            };
            ProviderComboBox.ItemsSource = providers;
            ProviderComboBox.SelectedValue = providerSetting ;
        }
        /// <summary>
        /// APIを実行
        /// </summary>
        /// <returns></returns>
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
                    ModernWpf.MessageBox.Show("OPENAI_API_KEY is not set.");
                    return;
                }

                ProviderType targetType = new ProviderType();
                string targetApiKey = "";
                string? targetBaseDomain = null;
                string? targetDeploymentId = null;
                string? targetApiVersion = null;
                switch(providerSetting)
                {
                    case "OpenAI":
                        targetType = ProviderType.OpenAi;
                        targetApiKey = apiKeySetting;
                        break;
                    case "Azure":
                        targetType = ProviderType.Azure;
                        targetApiKey = azureApiKeySetting;
                        targetBaseDomain = baseDomainSetting;
                        targetDeploymentId = deploymentIdSetting;
                        targetApiVersion = apiVersionSetting;
                        break;
                }
                var openAiService = new OpenAIService(new OpenAiOptions()
                {
                    ProviderType = targetType,
                    ApiKey = targetApiKey,
                    BaseDomain = targetBaseDomain,
                    DeploymentId = targetDeploymentId,
                    ApiVersion = targetApiVersion,
                });

                // デフォルトモデルを設定
                //openAiService.SetDefaultModelId("gpt-3.5-turbo");
                openAiService.SetDefaultModelId(modelSetting);

                // 今回の送信
                var userMessage = UserTextBox.Text;

                //instructionSettingをキーにinstructionListSettingの2列目を取得
                string selectInstructionContent = "";
                if (!String.IsNullOrEmpty(instructionSetting))
                {
                    string[] instructionList = instructionListSetting?.Cast<string>().Where((s, i) => i % 2 == 0).ToArray();
                    int index = Array.IndexOf(instructionList, instructionSetting);
                    selectInstructionContent = instructionListSetting[index, 1];
                }

                Debug.Print("----- Parameter -----");
                Debug.Print($"Temperature:{temperatureSetting}");
                Debug.Print("----- Contents of this message sent -----");
                Debug.Print(selectInstructionContent);
                Debug.Print(userMessage);

                List<ChatMessage> messages = new List<ChatMessage>();
                messages.AddRange(conversationHistory);
                messages.Add(ChatMessage.FromSystem(selectInstructionContent));
                messages.Add(ChatMessage.FromUser(userMessage));

                var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest()
                {
                    Messages = messages,
                    Temperature = temperatureSetting
                });

                if (completionResult.Successful)
                {
                    AssistantMarkdownText.Markdown = completionResult.Choices.First().Message.Content;

                    // トークン量を計算してツールチップで表示
                    string conversationHistoryString = "";
                    foreach (var item in conversationHistory)
                    {
                        conversationHistoryString += item.Content;
                    }
                    var conversationResultTokens = TokenizerGpt3.Encode(conversationHistoryString);
                    var instructionTokens = TokenizerGpt3.Encode(selectInstructionContent);
                    var userTokens = TokenizerGpt3.Encode(userMessage);
                    var responseTokens = TokenizerGpt3.Encode(completionResult.Choices.First().Message.Content);
                    var totalTokens = conversationResultTokens.Count() + instructionTokens.Count() + userTokens.Count() + responseTokens.Count();
                    string tooltip = "";
                    tooltip += $"Conversation History Tokens : {conversationResultTokens.Count()}\r\n";
                    tooltip += $"Instruction Tokens : {instructionTokens.Count()}\r\n";
                    tooltip += $"User Message Tokens : {userTokens.Count()}\r\n";
                    tooltip += $"AI Response Tokens : {responseTokens.Count()}\r\n";
                    tooltip += $"Total Tokens : {totalTokens}";
                    TokensLabel.Content = totalTokens;
                    TokensLabel.ToolTip = tooltip;

                    conversationHistory.Add(ChatMessage.FromUser(userMessage));
                    conversationHistory.Add(ChatMessage.FromAssistant(completionResult.Choices.First().Message.Content));

                    // その日のトークン使用量記録に追加
                    AddTokenUsage(totalTokens);

                    if (noticeFlgSetting)
                    {
                        new ToastContentBuilder()
                            .AddText("️AI responded back.")
                            .Show();
                    }

                    sw.Stop();
                    TimeLabel.Content = $"{sw.ElapsedMilliseconds} ms";

                    // 入力内容を消してプレースホルダーに入力内容を入れる
                    this.DataContext = new DataBind { PlaceHolder = userMessage };
                    UserTextBox.Text = "";
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
        /// <summary>
        /// メッセージボックスを開く
        /// </summary>
        /// <param name="title">タイトル</param>
        /// <param name="content">内容</param>
        private void ShowMessagebox(string title, string content)
        {
            var window = new Messagebox(title,content);
            window.Owner = this;
            window.ShowDialog();
        }
        /// <summary>
        /// 設定画面を開く
        /// </summary>
        /// <param name="targetSetting">設定対象</param>
        /// <param name="content">現在の設定</param>
        /// <param name="type">設定値の種別</param>
        /// <returns></returns>
        private string ShowSetting(string targetSetting, string content, string type)
        {
            var window = new Setting(targetSetting, content, type);
            window.Owner = this;
            bool result = (bool)window.ShowDialog();
            return result ? window.inputResult : "";
        }
        private void ShowTable()
        {
            int count = conversationHistory.Count;
            string[,] table = new string[count, 2];
            foreach (var item in conversationHistory)
            {
                table[conversationHistory.IndexOf(item), 0] = item.Role;
                table[conversationHistory.IndexOf(item), 1] = item.Content;
            }
            var window = new Table(table);
            window.Owner = this;
            window.ShowDialog();
        }
        public static string SerializeArray(string[,] array)
        {
            return JsonConvert.SerializeObject(array);
        }
        public static string[,] DeserializeArray(string serializedArray)
        {
            if (serializedArray == "" || serializedArray == null)
            {
                return new string[0, 0];
            }
            else
            {
                return JsonConvert.DeserializeObject<string[,]>(serializedArray);
            }
            //return JsonConvert.DeserializeObject<string[,]>(serializedArray);
        }
        /// <summary>
        /// Temperatureパラメータを設定する
        /// </summary>
        private void TemperatureSettingWindowOpen()
        {
            string result = ShowSetting("Temperature", temperatureSetting.ToString(), "number");
            if (result != "")
            {
                temperatureSetting = float.Parse(result);
            }
        }
        /// <summary>
        /// APIKeyを設定する
        /// </summary>
        private void APIKeySettingWindowOpen()
        {
            string result = ShowSetting("API Key (OpenAI)", apiKeySetting, "password");
            if (result != "")
            {
                apiKeySetting = result;
            }
        }
        /// <summary>
        /// 指示内容を設定する
        /// </summary>
        private void  InstructionSettingWindowOpen()
        {
            var window = new InstructionSettingWindow(instructionListSetting);
            window.Owner = this;
            bool result = (bool)window.ShowDialog();
            if (result)
            {
                instructionListSetting = result ? window.inputResult : null;
                //instructionListSettingの1列目を取得
                string[] instructionList = instructionListSetting?.Cast<string>().Where((s, i) => i % 2 == 0).ToArray();
                Array.Resize(ref instructionList, instructionList.Length + 1);
                instructionList[instructionList.Length - 1] = "";
                InstructionComboBox.ItemsSource = instructionList;
            }
        }
        private void AddTokenUsage(int token)
        {
            int rowCount = tokenUsageSetting.GetLength(0);
            int colCount = tokenUsageSetting.GetLength(1);
            if (tokenUsageSetting == null || rowCount == 0 || colCount == 0)
            {
                // 日付、プロバイダ、モデル、トークン量
                string[,] temp = new string[0, 4];
                tokenUsageSetting = temp;
            }


            string todayString = DateTime.Today.ToString("yyyy/MM/dd");
            string[,] tokenUsage = tokenUsageSetting;
            int tokenUsageCount = tokenUsage.GetLength(0);

            //今日のトークン使用量があるか
            bool todayTokenUsageExist = false;
            for (int i = 0; i < tokenUsageCount; i++)
            {
                if (tokenUsage[i, 0] == todayString && tokenUsage[i,1] == providerSetting && tokenUsage[i, 2] == ModelComboBox.Text)
                {
                    // トークン使用量を加算
                    tokenUsage[i, 3] = (int.Parse(tokenUsage[i, 3]) + token).ToString();
                    todayTokenUsageExist = true;
                }
            }
            //今日のトークン使用量がなければ追加
            if (!todayTokenUsageExist)
            {
                //Array.Resize(ref tokenUsage, tokenUsageCount + 1);
                tokenUsage = ResizeArray(tokenUsage, tokenUsageCount + 1, 4);
                tokenUsage[tokenUsageCount, 0] = todayString;
                tokenUsage[tokenUsageCount, 1] = providerSetting;
                tokenUsage[tokenUsageCount, 2] = ModelComboBox.Text;
                tokenUsage[tokenUsageCount, 3] = token.ToString();
            }
            tokenUsageSetting = tokenUsage;
            Properties.Settings.Default.TokenUsage = SerializeArray(tokenUsageSetting);
            Properties.Settings.Default.Save();
        }
        /// <summary>
        /// 他次元配列のサイズを変更する
        /// </summary>
        public static string[,] ResizeArray(string[,] originalArray, int newRowCount, int newColCount)
        {
            int originalRowCount = originalArray.GetLength(0);
            int originalColCount = originalArray.GetLength(1);

            string[,] newArray = new string[newRowCount, newColCount];

            for (int i = 0; i < Math.Min(originalRowCount, newRowCount); i++)
            {
                for (int j = 0; j < Math.Min(originalColCount, newColCount); j++)
                {
                    newArray[i, j] = originalArray[i, j];
                }
            }

            return newArray;
        }
        public static void InitialColorSet()
        {
            string theme = Properties.Settings.Default.Theme;
            if (theme == "Dark")
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            }
            else if (theme == "Light")
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            }
            else
            {
                ThemeManager.Current.ApplicationTheme = null;
            }
            
            string accentColor = Properties.Settings.Default.AccentColor;
            if (accentColor == "Default" || accentColor == "")
            {
                ThemeManager.Current.AccentColor = null;
            }
            else
            {
                var color = (Color)ColorConverter.ConvertFromString(accentColor);
                ThemeManager.Current.AccentColor = color;
            }
        }
    }
}
