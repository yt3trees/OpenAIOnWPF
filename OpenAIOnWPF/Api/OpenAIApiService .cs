using MdXaml;
using Microsoft.Toolkit.Uwp.Notifications;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.Tokenizer.GPT3;
using OpenAIOnWPF.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static OpenAIOnWPF.UtilityFunctions;

namespace OpenAIOnWPF
{
    public partial class MainWindow
    {
        bool resultFlg = true;
        private bool isProcessing = false;
        // 描画遅延対策
        private void DummySub() { }
        private void FlushWindowsMessageQueue()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(new Action(DummySub), DispatcherPriority.Background, new Object[] { });
        }
        string userMessage = "";
        string responseText = "";
        byte[] binaryImage = null;
        byte[] clipboardImage = null;
        string generatedTitle = "";
        bool titleGenerating = false;
        bool alertFlg = false;
        int dailyTotal = 0;
        string todayString = null;
        Guid newId;
        List<ChatMessage> tempMessages = new List<ChatMessage>();
        public static class ForTokenCalc
        {
            public static string oldConversationsToken { get; set; } = "";
            public static string systemPromptToken { get; set; } = "";
            public static string userPromptToken { get; set; } = "";
            public static string responseToken { get; set; } = "";
        }
        /// <summary>
        /// APIを実行
        /// </summary>
        /// <returns></returns>
        private async Task ProcessOpenAIAsync(string prompt)
        {
            Debug.Print("===== Start processing =====");
            if (isProcessing)
            {
                ModernWpf.MessageBox.Show("Processing is in progress.");
                return;
            }
            isProcessing = true;
            resultFlg = true;
            Prepare();
            MessageScrollViewer.ScrollToBottom();

            try
            {
                if (!RetrieveConfiguration())
                {
                    throw new Exception("ConfigurationName is not set.");
                }

                binaryImage = null;
                if (imageFilePath != null)
                {
                    binaryImage = await File.ReadAllBytesAsync(imageFilePath);
                }
                else if (clipboardImage != null)
                {
                    binaryImage = clipboardImage;
                }

                var openAiService = CreateOpenAiService(AppSettings.ProviderSetting
                                                        , AppSettings.ModelSetting
                                                        , AppSettings.ApiKeySetting
                                                        , AppSettings.BaseDomainSetting
                                                        , AppSettings.DeploymentIdSetting
                                                        , AppSettings.ApiVersionSetting);
                userMessage = prompt.Trim();
                var messages = PrepareMessages(prompt, binaryImage);
                tempMessages = messages;

                generatedTitle = "";
                if (AppSettings.UseTitleGenerationSetting && ConversationListBox.SelectedIndex == -1)
                {
                    _ = Task.Run(async () =>
                    {
                        string prompt = AppSettings.TitleGenerationPromptSetting
                                        .Replace("{Language}", AppSettings.TitleLanguageSetting)
                                        .Replace("{Prompt}", userMessage);
                        Debug.Print("----- Title generation Prompt -----");
                        Debug.Print(prompt);
                        Debug.Print("-----------------------------------");
                        await GenerateTitleAsync(prompt);
                    });
                }

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
                    if (AppSettings.BaseModelSetting == "o1")
                    {
                        OpenAI.ObjectModels.ResponseModels.ChatCompletionCreateResponse completionResult;
                        completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest()
                        {
                            Messages = messages,
                            MaxCompletionTokens = AppSettings.MaxTokensSetting
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
                        _cancellationTokenSource = new CancellationTokenSource();
                        Task.Run(async () => { await HandleCompletionResultStream(completionResult, _cancellationTokenSource.Token); });
                    }
                }
            }
            catch (Exception ex)
            {
                Reset();
                ModernWpf.MessageBox.Show(ex.ToString());
                throw new Exception($"{ex.Message}");
            }
            finally
            {
                Debug.Print("===== End of process =====");
            }
        }
        private void Prepare()
        {
            stopWatch.Start();
            TimeLabel.Content = "";
            TokensLabel.Content = "";
            ProgressRing.IsActive = true;
            UserTextBox.Text = "";
            ConversationListBox.IsEnabled = false;
            NewChatButton.IsEnabled = false;
            ConversationHistoryButton.IsEnabled = false;
            ConversationHistoryClearButton.IsEnabled = false;

            responseText = "";
            ExecButton.IsEnabled = false;
            TranslateButton.IsEnabled = false;

            ForTokenCalc.oldConversationsToken = "";
            ForTokenCalc.systemPromptToken = "";
            ForTokenCalc.userPromptToken = "";
            ForTokenCalc.responseToken = "";
        }
        private void Reset()
        {
            stopWatch.Stop();
            TimeLabel.Content = $"{stopWatch.ElapsedMilliseconds.ToString("N0")} ms";
            stopWatch.Reset();
            ExecButton.IsEnabled = true;
            TranslateButton.IsEnabled = true;
            ProgressRing.IsActive = false;
            ConversationListBox.IsEnabled = true;
            NewChatButton.IsEnabled = true;
            ConversationHistoryButton.IsEnabled = true;
            ConversationHistoryClearButton.IsEnabled = true;
            isProcessing = false;
            imageFilePath = null;
            clipboardImage = null;
            ImageFilePathLabel.Content = "";
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
                AppSettings.BaseModelSetting = rows[0]["BaseModel"].ToString();
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
        private OpenAIService CreateOpenAiService(string providerSetting, string model, string targetApiKey, string targetBaseDomain, string targetDeploymentId, string? targetApiVersion)
        {
            ProviderType targetType = new ProviderType();
            string tempTargetApiKey = "";
            string? tempTargetBaseDomain = null;
            string? tempTargetDeploymentId = null;
            string? tempTargetApiVersion = null;

            switch (providerSetting)
            {
                case "OpenAI":
                    targetType = ProviderType.OpenAi;
                    tempTargetApiKey = targetApiKey;
                    break;
                case "Azure":
                    targetType = ProviderType.Azure;
                    tempTargetApiKey = targetApiKey;
                    tempTargetBaseDomain = targetBaseDomain;
                    tempTargetDeploymentId = targetDeploymentId;
                    tempTargetApiVersion = string.IsNullOrEmpty(targetApiVersion) ? null : targetApiVersion;
                    break;
            }

            var openAiService = new OpenAIService(new OpenAiOptions()
            {
                ProviderType = targetType,
                ApiKey = tempTargetApiKey,
                BaseDomain = tempTargetBaseDomain,
                DeploymentId = tempTargetDeploymentId,
                ApiVersion = tempTargetApiVersion,
            });

            //openAiService.SetDefaultModelId("gpt-3.5-turbo");
            openAiService.SetDefaultModelId(model);

            return openAiService;
        }

        private List<ChatMessage> PrepareMessages(string userMessage, byte[]? image)
        {
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
            if (AppSettings.UseConversationHistoryFlg == true)
            {
                var selectedItems = ConversationListBox.SelectedItems;
                if (selectedItems.Count > 0)
                {
                    var selectedConversationHistory = selectedItems.Cast<ConversationHistory>().ToList();

                    foreach (var conversationHistory in selectedConversationHistory)
                    {
                        // 上限値を超えた分はメッセージに含めない
                        if (conversationHistory.Messages.Count > AppSettings.ConversationHistoryCountSetting)
                        {
                            var tempList = conversationHistory.Messages.ToList();
                            tempList = tempList.Skip(tempList.Count - AppSettings.ConversationHistoryCountSetting).ToList();
                            messages.AddRange(tempList);
                            foreach (var token in tempList)
                            {
                                ForTokenCalc.oldConversationsToken += token.Content;
                            }
                        }
                        else
                        {
                            foreach (var token in conversationHistory.Messages)
                            {
                                ForTokenCalc.oldConversationsToken += token.Content;
                            }
                            messages.AddRange(conversationHistory.Messages);
                        }
                    }
                }
            }
            if (!String.IsNullOrEmpty(selectInstructionContent))
            {
                messages.Add(ChatMessage.FromSystem(selectInstructionContent));
            }
            if (image == null)
            {
                messages.Add(ChatMessage.FromUser(userMessage));
            }
            else
            {
                messages.Add(ChatMessage.FromUser(
                    new List<MessageContent>
                    {
                        MessageContent.TextContent(userMessage),
                        MessageContent.ImageBinaryContent(image, "png")
                    }
                ));
            }
            ForTokenCalc.systemPromptToken = selectInstructionContent;
            ForTokenCalc.userPromptToken = userMessage;

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
                ForTokenCalc.responseToken = responseText;
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
            Reset();
        }
        private async Task HandleCompletionResultStream(IAsyncEnumerable<OpenAI.ObjectModels.ResponseModels.ChatCompletionCreateResponse>? completionResult, CancellationToken cancellationToken)
        {
            MarkdownScrollViewer markdownScrollViewer = null;
            await Dispatcher.InvokeAsync(() =>
            {
                try
                {

                    // 再生成ボタンを非表示にする
                    List<System.Windows.Controls.Button> foundButtons = new List<System.Windows.Controls.Button>();
                    foreach (var child in GetAllChildren(MessagesPanel))
                    {
                        if (child is System.Windows.Controls.Button button && (string)button.Tag == "RegenerateButton")
                        {
                            button.Visibility = Visibility.Collapsed;
                        }
                    }

                    // Userメッセージ
                    var messageElement = CreateMessageElement(userMessage, isUser: true, isLastMessage: false);
                    MessagesPanel.Children.Add(messageElement);

                    if (binaryImage != null)
                    {
                        string imageString = Convert.ToBase64String(binaryImage);
                        var messageElementImage = CreateMessageElement(userMessage, isUser: false, isLastMessage: false, imageString);
                        MessagesPanel.Children.Add(messageElementImage);
                    }

                    // Assistantメッセージ
                    FrameworkElement assistantMessageElement = null;

                    assistantMessageElement = CreateMessageElement("", isUser: false, isLastMessage: true); // 要素だけ生成しておく
                    MessagesPanel.Children.Add(assistantMessageElement);

                    // Grid内のMarkdownScrollViewer要素を検索
                    Grid assistantMessageGrid = assistantMessageElement as Grid;
                    if (assistantMessageGrid != null)
                    {
                        foreach (var child in assistantMessageGrid.Children)
                        {
                            if (child is MarkdownScrollViewer)
                            {
                                markdownScrollViewer = child as MarkdownScrollViewer;
                                markdownScrollViewer.Document.LineHeight = 1.0;
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"{ex.Message}");
                }
            });

            string resultText = "";
            try
            {
                await foreach (var completion in completionResult.WithCancellation(cancellationToken))
                {
                    //cancellationToken.ThrowIfCancellationRequested();

                    if (completion.Successful)
                    {
                        var firstChoice = completion.Choices.FirstOrDefault();
                        if (firstChoice == null)
                        {
                            continue;
                        }
                        resultText = firstChoice.Message.Content;
                        await Dispatcher.InvokeAsync(() =>
                        {
                            responseText += $"{resultText}";
                            markdownScrollViewer.Markdown += resultText;
                            markdownScrollViewer.Document.FontSize = Properties.Settings.Default.FontSize;
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
                        await Dispatcher.InvokeAsync(() =>
                        {
                            ModernWpf.MessageBox.Show($"{completion.Error.Code}: {completion.Error.Message}");
                        });
                        resultFlg = false;
                    }
                }
            }
            catch (OperationCanceledException) { }

            Debug.Print("----- Conversation History -----");
            tempMessages.Add(ChatMessage.FromAssistant(responseText));
            foreach (var item in tempMessages)
            {
                Debug.Print($"{item.Role}: {item.Content}");
            }

            await Dispatcher.InvokeAsync(() =>
            {
                ForTokenCalc.responseToken = responseText;

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

                if (alertFlg && Properties.Settings.Default.LastAlertDate != todayString)
                {
                    var result = ModernWpf.MessageBox.Show(
                        $"Daily token usage of {dailyTotal} exceeds the threshold of {Properties.Settings.Default.dailyTokenThreshold}! Do not show alerts for today again?",
                        "Token Usage Alert",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        Properties.Settings.Default.LastAlertDate = todayString;
                        Properties.Settings.Default.Save();
                    }
                }

                Reset();
            });
        }
        /// <summary>
        /// トークン量を計算
        /// </summary>
        private void CaluculateTokenUsage()
        {
            var conversationResultTokens = TokenizerGpt3.Encode(ForTokenCalc.oldConversationsToken);
            var instructionTokens = TokenizerGpt3.Encode(ForTokenCalc.systemPromptToken);
            var userTokens = TokenizerGpt3.Encode(ForTokenCalc.userPromptToken);
            var responseTokens = TokenizerGpt3.Encode(ForTokenCalc.responseToken);
            var inputTokens = conversationResultTokens.Count() + instructionTokens.Count() + userTokens.Count();
            var outputTokens = responseTokens.Count();
            var totalTokens = inputTokens + outputTokens;
            string tooltip = "";
            tooltip += $"Conversation History Tokens : {conversationResultTokens.Count().ToString("N0")}\r\n";
            tooltip += $"System Prompt Tokens : {instructionTokens.Count().ToString("N0")}\r\n";
            tooltip += $"User Message Tokens : {userTokens.Count().ToString("N0")}\r\n";
            tooltip += $"AI Response Tokens : {responseTokens.Count().ToString("N0")}\r\n";
            tooltip += $"Total Tokens : {totalTokens.ToString("N0")}";
            TokensLabel.Content = totalTokens.ToString("N0");
            TokensLabel.ToolTip = tooltip;

            // 既存の会話履歴に追加する場合
            if (ConversationListBox.SelectedIndex != -1)
            {
                var selectedConversation = (ConversationHistory)ConversationListBox.SelectedItem;
                Guid selectedId = selectedConversation.ID;

                var conversation = AppSettings.ConversationManager.Histories.FirstOrDefault(c => c.ID == selectedId);
                if (conversation != null)
                {
                    if (binaryImage == null)
                    {
                        conversation.Messages.Add(ChatMessage.FromUser(userMessage));
                    }
                    else
                    {
                        conversation.Messages.Add(ChatMessage.FromUser(
                            new List<MessageContent>
                            {
                                MessageContent.TextContent(userMessage),
                                MessageContent.ImageBinaryContent(binaryImage, "png")
                            }
                        ));
                    }
                    conversation.Messages.Add(ChatMessage.FromAssistant(responseText));
                }
                RefreshConversationList(); // Sort
            }
            // 何も選択していない場合
            if (ConversationListBox.SelectedIndex == -1)
            {
                string cleanedUserMessage = userMessage.Replace("\n", "").Replace("\r", "");
                string title = "";
                if (AppSettings.UseTitleGenerationSetting)
                {
                    if (!string.IsNullOrEmpty(generatedTitle))
                    {
                        title = generatedTitle;
                    }
                    else
                    {
                        // 会話終了時点でタイトルが未生成の場合は一旦仮の値を入れる
                        title = "generating...";
                        titleGenerating = true;
                    }
                }
                else
                {
                    title = cleanedUserMessage.Length > 20 ? cleanedUserMessage.Substring(0, 20) + "..." : cleanedUserMessage;
                }

                ConversationHistory newHistory;
                if (binaryImage == null)
                {
                    newHistory = new ConversationHistory()
                    {
                        Title = title,
                        Messages = new ObservableCollection<ChatMessage>()
                        {
                            ChatMessage.FromUser(userMessage),
                            ChatMessage.FromAssistant(responseText)
                        }
                    };
                }
                else
                {
                    newHistory = new ConversationHistory()
                    {
                        Title = title,
                        Messages = new ObservableCollection<ChatMessage>()
                        {
                            ChatMessage.FromUser(
                            new List<MessageContent>
                                {
                                    MessageContent.TextContent(userMessage),
                                    MessageContent.ImageBinaryContent(binaryImage, "png")
                                }
                            ),
                            ChatMessage.FromAssistant(responseText)
                        }
                    };
                }
                AppSettings.ConversationManager.Histories.Add(newHistory);

                // AIからの回答終了時点でタイトルがセットできていない場合は別処理でセットするためIDを一旦退避
                if (titleGenerating)
                {
                    newId = newHistory.ID;
                }

                RefreshConversationList(); // Sort
                ConversationListBox.SelectedIndex = 0;
            }

            string model = AppSettings.ModelSetting != "" ? AppSettings.ModelSetting : AppSettings.DeploymentIdSetting;
            // その日のトークン使用量記録に追加
            AddTokenUsage(totalTokens, inputTokens, outputTokens, model, AppSettings.ProviderSetting);
        }
        private void AddTokenUsage(int totalToken, int inputTokens, int outputTokens, string model, string provider)
        {
            int rowCount = AppSettings.TokenUsageSetting.GetLength(0);
            int colCount = AppSettings.TokenUsageSetting.GetLength(1);
            if (AppSettings.TokenUsageSetting == null || rowCount == 0 || colCount == 0)
            {
                // 日付、プロバイダ、モデル、合計トークン量、入力トークン量、出力トークン量
                string[,] temp = new string[0, 5];
                AppSettings.TokenUsageSetting = temp;
            }

            string[,] oldTokenUsage = AppSettings.TokenUsageSetting; // 既存の配列
            int rows = oldTokenUsage.GetLength(0); // 既存の配列の行数
            int cols = oldTokenUsage.GetLength(1); // 既存の配列の列数
            // 新しい配列を作成（行数は同じ、列数は2つ増やす）
            string[,] newTokenUsage = new string[rows, cols + 2];
            // 既存のデータを新しい配列にコピーし、新しい要素を追加
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    newTokenUsage[i, j] = oldTokenUsage[i, j];
                }
                // 新しい列のデータを追加
                newTokenUsage[i, cols] = "0";
                newTokenUsage[i, cols + 1] = "0";
            }

            todayString = DateTime.Today.ToString("yyyy/MM/dd");
            string[,] tokenUsage = AppSettings.TokenUsageSetting;
            int tokenUsageCount = tokenUsage.GetLength(0);
            dailyTotal = 0;

            //今日のトークン使用量があるか
            bool todayTokenUsageExist = false;
            for (int i = 0; i < tokenUsageCount; i++)
            {
                if (tokenUsage[i, 0] == todayString)
                {
                    dailyTotal += int.Parse(tokenUsage[i, 3]);
                    dailyTotal += totalToken;
                    if (tokenUsage[i, 1] == provider && tokenUsage[i, 2] == model)
                    {
                        {
                            // トークン使用量を加算
                            tokenUsage[i, 3] = (int.Parse(tokenUsage[i, 3]) + totalToken).ToString();
                            tokenUsage[i, 4] = (int.Parse(tokenUsage[i, 4]) + inputTokens).ToString();
                            tokenUsage[i, 5] = (int.Parse(tokenUsage[i, 5]) + outputTokens).ToString();
                            todayTokenUsageExist = true;
                        }
                    }
                }
            }
            //今日のトークン使用量がなければ追加
            if (!todayTokenUsageExist)
            {
                tokenUsage = ResizeArray(tokenUsage, tokenUsageCount + 1, 6);
                tokenUsage[tokenUsageCount, 0] = todayString;
                tokenUsage[tokenUsageCount, 1] = provider;
                tokenUsage[tokenUsageCount, 2] = model;
                tokenUsage[tokenUsageCount, 3] = totalToken.ToString();
                tokenUsage[tokenUsageCount, 4] = inputTokens.ToString();
                tokenUsage[tokenUsageCount, 5] = outputTokens.ToString();
                dailyTotal += totalToken;
            }
            AppSettings.TokenUsageSetting = tokenUsage;
            Properties.Settings.Default.TokenUsage = SerializeArray(AppSettings.TokenUsageSetting);
            Properties.Settings.Default.Save();

            alertFlg = false;
            if (dailyTotal > Properties.Settings.Default.dailyTokenThreshold)
            {
                alertFlg = true;
            }
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
        public async Task GenerateTitleAsync(string userMessage)
        {
            try
            {
                Debug.WriteLine($"GenerateTitleAsync started on thread ID: {Thread.CurrentThread.ManagedThreadId}");

                string configName = AppSettings.ModelForTitleGenerationSetting;
                DataRow[] rows = AppSettings.ConfigDataTable.Select("ConfigurationName = '" + configName + "'");
                var ProviderSetting = rows[0]["Provider"].ToString();
                var ModelSetting = rows[0]["Model"].ToString();
                var ApiKeySetting = rows[0]["APIKey"].ToString();
                var DeploymentIdSetting = rows[0]["DeploymentId"].ToString();
                var BaseDomainSetting = rows[0]["BaseDomain"].ToString();
                var ApiVersionSetting = rows[0]["ApiVersion"].ToString();
                var BaseModelSetting = rows[0]["BaseModel"].ToString();
                float TemperatureSetting;
                int MaxTokensSetting;
                if (string.IsNullOrEmpty(rows[0]["Temperature"].ToString()) == false)
                {
                    TemperatureSetting = float.Parse(rows[0]["Temperature"].ToString());
                }
                else
                {
                    TemperatureSetting = 1;
                }
                if (string.IsNullOrEmpty(rows[0]["MaxTokens"].ToString()) == false)
                {
                    MaxTokensSetting = int.Parse(rows[0]["MaxTokens"].ToString());
                }
                else
                {
                    MaxTokensSetting = 2048;
                }

                var openAiService = CreateOpenAiService(ProviderSetting
                                                        , ModelSetting
                                                        , ApiKeySetting
                                                        , BaseDomainSetting
                                                        , DeploymentIdSetting
                                                        , ApiVersionSetting);

                List<ChatMessage> messages = new List<ChatMessage>();
                messages.Add(ChatMessage.FromUser(userMessage));

                OpenAI.ObjectModels.ResponseModels.ChatCompletionCreateResponse completionResult;
                if (BaseModelSetting == "o1")
                {
                    completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest()
                    {
                        Messages = messages,
                        MaxCompletionTokens = MaxTokensSetting
                    });
                }
                else
                {
                    completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest()
                    {
                        Messages = messages,
                        Temperature = TemperatureSetting,
                        MaxTokens = MaxTokensSetting
                    });
                }

                HandleCompletionResultForTitle(completionResult);

                string model = ModelSetting != "" ? ModelSetting : DeploymentIdSetting;
                var userMessageTokens = TokenizerGpt3.Encode(userMessage);
                var responseTokens = TokenizerGpt3.Encode(generatedTitle);
                var totalTokens = userMessageTokens.Count() + responseTokens.Count();
                AddTokenUsage(totalTokens, userMessageTokens.Count(), responseTokens.Count(), model, ProviderSetting);

                if (titleGenerating)
                {
                    var historyToUpdate = AppSettings.ConversationManager.Histories.FirstOrDefault(history => history.ID == newId);
                    if (historyToUpdate != null)
                    {
                        historyToUpdate.Title = generatedTitle;
                    }
                    await Dispatcher.InvokeAsync(() =>
                    {
                        RefreshConversationList();
                    });
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    ModernWpf.MessageBox.Show(ex.Message);
                });
            }
        }
        private void HandleCompletionResultForTitle(OpenAI.ObjectModels.ResponseModels.ChatCompletionCreateResponse? completionResult)
        {
            if (completionResult.Successful)
            {
                generatedTitle = completionResult.Choices.First().Message.Content;
                Debug.Print("===== Generated Title =====");
                Debug.Print(generatedTitle);
                Debug.Print("===========================");
            }
            else
            {
                generatedTitle = "Error!";
                if (completionResult.Error == null)
                {
                    throw new Exception("Unknown Error");
                }
                else if (completionResult.Error.Message != null)
                {
                    throw new Exception($"Title generation Error: {completionResult.Error.Message}");
                }
            }
        }
    }
}
