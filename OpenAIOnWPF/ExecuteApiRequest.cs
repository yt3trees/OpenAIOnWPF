﻿using Markdig;
using Microsoft.Toolkit.Uwp.Notifications;
using ModernWpf;
using Newtonsoft.Json;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.Tokenizer.GPT3;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using static OpenAIOnWPF.UtilityFunctions;

namespace OpenAIOnWPF
{
    public partial class MainWindow
    {
        bool resultFlg = true;
        string responseText = "";
        // 描画遅延対策
        private void DummySub() { }
        private void FlushWindowsMessageQueue()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(new Action(DummySub), DispatcherPriority.Background, new Object[] { });
        }
        /// <summary>
        /// APIを実行
        /// </summary>
        /// <returns></returns>
        private async Task ProcessOpenAIAsync()
        {
            Debug.Print("===== Start processing =====");
            resultFlg = true;
            PrepareUI();
            MessageScrollViewer.ScrollToBottom();

            try
            {
                if (!RetrieveConfiguration())
                {
                    ModernWpf.MessageBox.Show("ConfigurationName is not set.");
                    return;
                }

                var openAiService = CreateOpenAiService();
                var messages = PrepareMessages();

                if (1 == 2)
                {
                    var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest()
                    {
                        Messages = messages,
                        Temperature = AppSettings.TemperatureSetting,
                        MaxTokens = AppSettings.MaxTokensSetting
                    });
                    HandleCompletionResult(completionResult);
                }
                else
                {
                    var completionResult = openAiService.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest
                    {
                        Messages = messages,
                        Temperature = AppSettings.TemperatureSetting,
                        MaxTokens = AppSettings.MaxTokensSetting
                    });
                    HandleCompletionResultStream(completionResult);
                }
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.ToString());
            }
            finally
            {
                Debug.Print("===== End of process =====");
            }
        }
        private void PrepareUI()
        {
            stopWatch.Start();
            TimeLabel.Content = "";
            TokensLabel.Content = "";
            ProgressRing.IsActive = true;

            //AssistantMarkdownText.Markdown = "";
            responseText = "";
            ExecButton.IsEnabled = false;
        }
        private void ResetUI()
        {
            stopWatch.Stop();
            TimeLabel.Content = $"{stopWatch.ElapsedMilliseconds} ms";
            stopWatch.Reset();
            ExecButton.IsEnabled = true;
            ProgressRing.IsActive = false;
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
                    targetApiVersion = string.IsNullOrEmpty(AppSettings.ApiVersionSetting) ? null : AppSettings.ApiVersionSetting;
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
            // システムプロンプトペインが開かれている場合はペイン内のテキストボックスの値をシステムプロンプトとして使用する
            if (AppSettings.IsSystemPromptColumnVisible == true)
            {
                selectInstructionContent = SystemPromptContentsTextBox.Text;
            }
            else if (!String.IsNullOrEmpty(AppSettings.InstructionSetting))
            {
                //instructionSettingをキーにAppSettins.InstructionListSettingの2列目を取得
                string[] instructionList = AppSettings.InstructionListSetting?.Cast<string>().Where((s, i) => i % 2 == 0).ToArray();
                int index = Array.IndexOf(instructionList, AppSettings.InstructionSetting);
                selectInstructionContent = AppSettings.InstructionListSetting[index, 1];
            }
            else
            {
                selectInstructionContent = "";
            }

            Debug.Print("----- Parameter -----");
            Debug.Print($"Temperature:{AppSettings.TemperatureSetting}");
            Debug.Print("----- Contents of this message sent -----");
            Debug.Print(selectInstructionContent);
            Debug.Print(userMessage);

            List<ChatMessage> messages = new List<ChatMessage>();
            if (AppSettings.UseConversationHistoryFlg == true && AppSettings.ConversationHistory != null)
            {
                messages.AddRange(AppSettings.ConversationHistory);
            }
            messages.Add(ChatMessage.FromSystem(selectInstructionContent));
            messages.Add(ChatMessage.FromUser(userMessage));

            return messages;
        }
        /// <summary>
        /// レスポンスを受け取って処理する
        /// </summary>
        private void HandleCompletionResult(OpenAI.ObjectModels.ResponseModels.ChatCompletionCreateResponse? completionResult)
        {
            if (completionResult.Successful)
            {
                //AssistantMarkdownText.Markdown = completionResult.Choices.First().Message.Content;
                responseText = completionResult.Choices.First().Message.Content;
                CaluculateTokenUsage();
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
                resultFlg = false;
                ModernWpf.MessageBox.Show($"{completionResult.Error.Code}: {completionResult.Error.Message}");
            }
            ResetUI();
        }
        private async Task HandleCompletionResultStream(IAsyncEnumerable<OpenAI.ObjectModels.ResponseModels.ChatCompletionCreateResponse>? completionResult)
        {
            // Userメッセージ
            var messageElement = CreateMessageElement(userMessage, isUser: true);
            MessagesPanel.Children.Add(messageElement);

            // Assistantメッセージ
            FrameworkElement assistantMessageElement  = null;
            await Dispatcher.InvokeAsync(() =>
            {
                assistantMessageElement = CreateMessageElement("", isUser: false); // 要素だけ生成しておく
                MessagesPanel.Children.Add(assistantMessageElement );
            });
            // Grid内のRichTextBox要素を検索
            Grid assistantMessageGrid = assistantMessageElement as Grid;
            System.Windows.Controls.RichTextBox richTextBox = null;
            if (assistantMessageGrid != null)
            {
                foreach (var child in assistantMessageGrid.Children)
                {
                    if (child is System.Windows.Controls.RichTextBox)
                    {
                        richTextBox = child as System.Windows.Controls.RichTextBox;
                        richTextBox.Document.LineHeight = 1.0;
                        break;
                    }
                }
            }
            
            string resultText = "";
            await foreach (var completion in completionResult)
            {
                if (completion.Successful)
                {
                    resultText = completion.Choices.First().Message.Content;
                    await Dispatcher.InvokeAsync(() =>
                    {
                        responseText += $"{resultText}";
                        richTextBox.AppendText(resultText);
                        FlushWindowsMessageQueue(); // 描画遅延対策
                    });
                }
                else
                {
                    if (completion.Error == null)
                    {
                        throw new Exception("Unknown Error");
                    }
                    resultText = $"{completion.Error.Code}: {completion.Error.Message}";
                    ModernWpf.MessageBox.Show($"{completion.Error.Code}: {completion.Error.Message}");
                    resultFlg = false;
                }
            }
            var pipeline = new MarkdownPipelineBuilder()
            .UseSoftlineBreakAsHardlineBreak()
            .UseAdvancedExtensions()
            .Build();

            var flowDocument = Markdig.Wpf.Markdown.ToFlowDocument(responseText, pipeline);
            richTextBox.Document = flowDocument;

            if (resultFlg)
            {
                CaluculateTokenUsage();
            }
            if (AppSettings.NoticeFlgSetting && resultFlg)
            {
                new ToastContentBuilder()
                    .AddText("️AI responded back.")
                    .Show();
            }
            ResetUI();
        }
        /// <summary>
        /// トークン量を計算
        /// </summary>
        private void CaluculateTokenUsage()
        {
            string conversationHistoryString = "";
            if (AppSettings.UseConversationHistoryFlg == true && AppSettings.ConversationHistory != null)
            {
                foreach (var item in AppSettings.ConversationHistory)
                {
                    conversationHistoryString += item.Content;
                }
            }
            var conversationResultTokens = TokenizerGpt3.Encode(conversationHistoryString);
            var instructionTokens = TokenizerGpt3.Encode(selectInstructionContent);
            var userTokens = TokenizerGpt3.Encode(userMessage);
            var responseTokens = TokenizerGpt3.Encode(responseText );
            var totalTokens = conversationResultTokens.Count() + instructionTokens.Count() + userTokens.Count() + responseTokens.Count();
            string tooltip = "";
            tooltip += $"Conversation History Tokens : {conversationResultTokens.Count()}\r\n";
            tooltip += $"Instruction Tokens : {instructionTokens.Count()}\r\n";
            tooltip += $"User Message Tokens : {userTokens.Count()}\r\n";
            tooltip += $"AI Response Tokens : {responseTokens.Count()}\r\n";
            tooltip += $"Total Tokens : {totalTokens}";
            TokensLabel.Content = totalTokens;
            TokensLabel.ToolTip = tooltip;


            if (AppSettings.ConversationHistory == null)
            {
                AppSettings.ConversationHistory = new List<ChatMessage>();
            }
            AppSettings.ConversationHistory.Add(ChatMessage.FromUser(userMessage));
            AppSettings.ConversationHistory.Add(ChatMessage.FromAssistant(responseText));
            // Q:ConversationHistoryがnullの場合に上記がエラーになってしまいます。問題ない書き方にしてください

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

            string conversationHistoryJson = JsonConvert.SerializeObject(AppSettings.ConversationHistory);
            Properties.Settings.Default.ConversationHistory = conversationHistoryJson;
            Properties.Settings.Default.Save();

            // その日のトークン使用量記録に追加
            AddTokenUsage(totalTokens);
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
