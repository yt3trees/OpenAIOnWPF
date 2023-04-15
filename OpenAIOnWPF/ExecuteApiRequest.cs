using Microsoft.Toolkit.Uwp.Notifications;
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
using static OpenAIOnWPF.UtilityFunctions;

namespace OpenAIOnWPF
{
    public partial class MainWindow
    {
        /// <summary>
        /// APIを実行
        /// </summary>
        /// <returns></returns>
        private async Task ProcessOpenAIAsync()
        {
            Debug.Print("===== Start processing =====");

            PrepareUI();

            try
            {
                if (!RetrieveConfiguration())
                {
                    ModernWpf.MessageBox.Show("ConfigurationName is not set.");
                    return;
                }

                var openAiService = CreateOpenAiService();
                var messages = PrepareMessages();

                var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest()
                {
                    Messages = messages,
                    Temperature = AppSettings.TemperatureSetting,
                    MaxTokens = AppSettings.MaxTokensSetting
                });

                HandleCompletionResult(completionResult);

            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.ToString());
            }
            finally
            {
                ResetUI();
                Debug.Print("===== End of process =====");
            }
        }
        private void PrepareUI()
        {
            stopWatch.Start();
            TimeLabel.Content = "";
            TokensLabel.Content = "";
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;

            AssistantMarkdownText.Markdown = "";
            ExecButton.IsEnabled = false;
            ExecButton.Content = "Sending...";
        }
        private void ResetUI()
        {
            stopWatch.Stop();
            TimeLabel.Content = $"{stopWatch.ElapsedMilliseconds} ms";
            stopWatch.Reset();
            ExecButton.IsEnabled = true;
            ExecButton.Content = "Send";
            ProgressBar.Visibility = Visibility.Collapsed;
            ProgressBar.IsIndeterminate = false;
            UserTextBox.Text = "";
        }
        /// <summary>
        /// 選択している設定の内容を取得
        /// </summary>
        private bool RetrieveConfiguration()
        {
            string configName = ConfigurationComboBox.Text;
            DataRow[] rows = AppSettings.ConfigDataTable.Select("ConfigurationName = '" + configName + "'");
            if (rows.Length > 0)
            {
                AppSettings.ProviderSetting = rows[0]["Provider"].ToString();
                AppSettings.ModelSetting = rows[0]["Model"].ToString();
                AppSettings.ApiKeySetting = rows[0]["APIKey"].ToString();
                AppSettings.DeploymentIdSetting = rows[0]["DeploymentId"].ToString();
                AppSettings.BaseDomainSetting = rows[0]["BaseDomain"].ToString();
                AppSettings.ApiVersionSetting = rows[0]["ApiVersion"].ToString();
                if (string.IsNullOrEmpty(rows[0]["Temperature"].ToString()) == false)
                {
                    AppSettings.TemperatureSetting = float.Parse(rows[0]["Temperature"].ToString());
                }
                else
                {
                    AppSettings.TemperatureSetting = 1;
                }
                if (string.IsNullOrEmpty(rows[0]["MaxTokens"].ToString()) == false)
                {
                    AppSettings.MaxTokensSetting = int.Parse(rows[0]["MaxTokens"].ToString());
                }
                else
                {
                    AppSettings.MaxTokensSetting = 2048;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        private OpenAIService CreateOpenAiService()
        {
            ProviderType targetType = new ProviderType();
            string targetApiKey = "";
            string? targetBaseDomain = null;
            string? targetDeploymentId = null;
            string? targetApiVersion = null;

            switch (AppSettings.ProviderSetting)
            {
                case "OpenAI":
                    targetType = ProviderType.OpenAi;
                    targetApiKey = AppSettings.ApiKeySetting;
                    break;
                case "Azure":
                    targetType = ProviderType.Azure;
                    targetApiKey = AppSettings.ApiKeySetting;
                    targetBaseDomain = AppSettings.BaseDomainSetting;
                    targetDeploymentId = AppSettings.DeploymentIdSetting;
                    targetApiVersion = AppSettings.ApiVersionSetting;
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

            //openAiService.SetDefaultModelId("gpt-3.5-turbo");
            openAiService.SetDefaultModelId(AppSettings.ModelSetting);

            return openAiService;
        }

        private List<ChatMessage> PrepareMessages()
        {
            userMessage = UserTextBox.Text;
            //instructionSettingをキーにAppSettins.InstructionListSettingの2列目を取得
            if (!String.IsNullOrEmpty(AppSettings.InstructionSetting))
            {
                string[] instructionList = AppSettings.InstructionListSetting?.Cast<string>().Where((s, i) => i % 2 == 0).ToArray();
                int index = Array.IndexOf(instructionList, AppSettings.InstructionSetting);
                selectInstructionContent = AppSettings.InstructionListSetting[index, 1];
            }

            Debug.Print("----- Parameter -----");
            Debug.Print($"Temperature:{AppSettings.TemperatureSetting}");
            Debug.Print("----- Contents of this message sent -----");
            Debug.Print(selectInstructionContent);
            Debug.Print(userMessage);

            List<ChatMessage> messages = new List<ChatMessage>();
            messages.AddRange(AppSettings.ConversationHistory);
            messages.Add(ChatMessage.FromSystem(selectInstructionContent));
            messages.Add(ChatMessage.FromUser(userMessage));

            return messages;
        }
        /// <summary>
        /// レスポンスを受け取って処理する
        /// </summary>
        private void HandleCompletionResult(OpenAI.GPT3.ObjectModels.ResponseModels.ChatCompletionCreateResponse? completionResult)
        {
            if (completionResult.Successful)
            {
                AssistantMarkdownText.Markdown = completionResult.Choices.First().Message.Content;

                // トークン量を計算してツールチップで表示
                string conversationHistoryString = "";
                foreach (var item in AppSettings.ConversationHistory)
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

                AppSettings.ConversationHistory.Add(ChatMessage.FromUser(userMessage));
                AppSettings.ConversationHistory.Add(ChatMessage.FromAssistant(completionResult.Choices.First().Message.Content));

                // 入力内容を消してプレースホルダーに入力内容を入れる
                this.DataContext = new DataBind { PlaceHolder = userMessage };

                // 閾値超え会話履歴を削除
                if (AppSettings.ConversationHistory.Count > AppSettings.ConversationHistoryCountSetting)
                {
                   AppSettings.ConversationHistory.RemoveRange(0, AppSettings.ConversationHistory.Count - AppSettings.ConversationHistoryCountSetting);
                    Debug.Print($"Deleted because conversation history exceeded {AppSettings.ConversationHistoryCountSetting} conversations.");
                }
                Debug.Print("----- Conversation History -----");
                foreach (var item in AppSettings.ConversationHistory)
                {
                    Debug.Print($"{item.Role}: {item.Content}");
                }

                // その日のトークン使用量記録に追加
                AddTokenUsage(totalTokens);

                if (AppSettings.NoticeFlgSetting)
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
                if (AppSettings.NoticeFlgSetting)
                {
                    new ToastContentBuilder()
                        .AddText("️An error has occurred.")
                        .Show();
                }
                ModernWpf.MessageBox.Show($"{completionResult.Error.Code}: {completionResult.Error.Message}");
            }
        }
        private void AddTokenUsage(int token)
        {
            int rowCount = AppSettings.TokenUsageSetting.GetLength(0);
            int colCount = AppSettings.TokenUsageSetting.GetLength(1);
            if (AppSettings.TokenUsageSetting == null || rowCount == 0 || colCount == 0)
            {
                // 日付、プロバイダ、モデル、トークン量
                string[,] temp = new string[0, 4];
                AppSettings.TokenUsageSetting = temp;
            }

            string todayString = DateTime.Today.ToString("yyyy/MM/dd");
            string[,] tokenUsage = AppSettings.TokenUsageSetting;
            int tokenUsageCount = tokenUsage.GetLength(0);

            // OpenAIの場合はモデル、AzureOpenAIの場合はデプロイメントIDで集計する
            string model;
            if (AppSettings.ModelSetting != "")
            {
                model = AppSettings.ModelSetting;
            }
            else
            {
                model = AppSettings.DeploymentIdSetting;
            }

            //今日のトークン使用量があるか
            bool todayTokenUsageExist = false;
            for (int i = 0; i < tokenUsageCount; i++)
            {
                if (tokenUsage[i, 0] == todayString && tokenUsage[i, 1] == AppSettings.ProviderSetting && tokenUsage[i, 2] == model)
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
                tokenUsage[tokenUsageCount, 1] = AppSettings.ProviderSetting;
                tokenUsage[tokenUsageCount, 2] = model;
                tokenUsage[tokenUsageCount, 3] = token.ToString();
            }
            AppSettings.TokenUsageSetting = tokenUsage;
            Properties.Settings.Default.TokenUsage = SerializeArray(AppSettings.TokenUsageSetting);
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
    }
}
