﻿using Markdig;
using Markdig.Wpf;
using Microsoft.Toolkit.Uwp.Notifications;
using ModernWpf;
using Newtonsoft.Json;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.Tokenizer.GPT3;
using OpenAIOnWPF.Model;
using SourceChord.FluentWPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Input.Manipulations;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using static OpenAIOnWPF.UtilityFunctions;

namespace OpenAIOnWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        string selectInstructionContent = "";
        Stopwatch stopWatch = new Stopwatch();
        private bool gKeyPressed;

        public MainWindow()
        {
            InitializeComponent();
            RecoverWindowBounds();
            InitializeSettings();
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var collectionViewSource = FindResource("SortedConversations") as CollectionViewSource;
            if (collectionViewSource != null)
            {
                collectionViewSource.Source = AppSettings.ConversationManager.Histories;
                ConversationListBox.ItemsSource = collectionViewSource.View;
            }
        }
        private void InitializeSettings()
        {
            InitialColorSet();
            ToastNotificationManagerCompat.OnActivated += this.ToastNotificationManagerCompat_OnActivated;
            UserTextBox.Focus();
            NoticeToggleSwitch.IsOn = AppSettings.NoticeFlgSetting;

            AppSettings.ConversationManager = LoadConversationsFromJson();
            if (AppSettings.ConversationManager.Histories == null)
            {
                AppSettings.ConversationManager.Histories = new ObservableCollection<ConversationHistory>();
            }
            else
            {
                var selectedConversation = AppSettings.ConversationManager.Histories.FirstOrDefault(ch => ch.IsSelected);
                if (selectedConversation != null)
                {
                    ConversationListBox.SelectedItem = selectedConversation;
                }
            }

            // Settingsから指示内容リストを取得しセット
            SystemPromptComboBox.ItemsSource = SetupInstructionComboBox();
            SystemPromptComboBox.Text = String.IsNullOrEmpty(AppSettings.InstructionSetting) ? "" : AppSettings.InstructionSetting;
            SystemPromptComboBox2.ItemsSource = SetupInstructionComboBox();
            SystemPromptComboBox2.Text = String.IsNullOrEmpty(AppSettings.InstructionSetting) ? "" : AppSettings.InstructionSetting;

            var appSettings = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal);
            Debug.Print("Path to save the configuration file:" + appSettings.FilePath);

            InitializeConfigDataTable();

            ConfigurationComboBox.ItemsSource = AppSettings.ConfigDataTable.AsEnumerable().Select(x => x.Field<string>("ConfigurationName")).ToList();
            ConfigurationComboBox.Text = AppSettings.SelectConfigSetting;

            UseConversationHistoryToggleSwitch.IsOn = AppSettings.UseConversationHistoryFlg;

            MessageScrollViewer.ScrollToBottom();

            InitializeSystemPromptColumn();

            if (AppSettings.TranslationAPIUseFlg == true)
            {
                TranslateAPIGridColumn.Width = new GridLength(1, GridUnitType.Auto);
            }
            else
            {
                TranslateAPIGridColumn.Width = new GridLength(0);
            }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
            {
                var window = new ColorSettings();
                window.Owner = this;
                window.ShowDialog();
            }
            if (e.Key == Key.F3)
            {
                var window = new TranslationAPISettingWindow();
                window.Owner = this;
                window.ShowDialog();
            }
            if (e.Key == Key.F4)
            {
                var window = new ConfigSettingWindow();
                window.Owner = this;
                window.ShowDialog();
                ConfigurationComboBox.ItemsSource = AppSettings.ConfigDataTable.AsEnumerable().Select(x => x.Field<string>("ConfigurationName")).ToList();
            }
            if (e.Key == Key.F5)
            {
                ShowTable();
            }
        }
        private void AcrylicWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (ConversationListBox.SelectedIndex > 0)
                {
                    ConversationListBox.SelectedIndex--;
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Down && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (ConversationListBox.SelectedIndex < ConversationListBox.Items.Count - 1)
                {
                    ConversationListBox.SelectedIndex++;
                }
                e.Handled = true;
            }
            else if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
            {
                NewChatButton_Click(sender, e);
            }
        }
        private void UserTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                _ = ProcessOpenAIAsync();
            }
            else if (e.Key == Key.Enter && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt))
            {
                if (AppSettings.TranslationAPIUseFlg == true)
                {
                    TranslateButton_Click(sender, e);
                }
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
        private void NoticeToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            AppSettings.NoticeFlgSetting = (bool)NoticeToggleSwitch.IsOn;
        }
        private void TokensLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShowMessagebox("Tokens", TokensLabel.ToolTip.ToString());
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }
        private void ConfigurationComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ConfigurationComboBox.SelectedItem == null) return;
            AppSettings.SelectConfigSetting = ConfigurationComboBox.SelectedItem.ToString();
        }
        private void SystemPromptComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // SystemPromptComboBox2で選択しているのと同じ内容をInstructionComboBoxにセット
            SystemPromptComboBox2.SelectedIndex = SystemPromptComboBox.SelectedIndex;
            if (SystemPromptComboBox.SelectedItem == "")
            {
                AppSettings.InstructionSetting = "";
                return;
            }
            AppSettings.InstructionSetting = SystemPromptComboBox.SelectedItem.ToString();
            // ツールチップに内容を表示
            string selectInstructionContent = "";
            if (!String.IsNullOrEmpty(AppSettings.InstructionSetting))
            {
                string[] instructionList = AppSettings.InstructionListSetting?.Cast<string>().Where((s, i) => i % 2 == 0).ToArray();
                int index = Array.IndexOf(instructionList, AppSettings.InstructionSetting);
                selectInstructionContent = AppSettings.InstructionListSetting[index, 1];
            }
            SystemPromptComboBox.ToolTip = "# " + AppSettings.InstructionSetting + "\r\n"
                                          + selectInstructionContent;
        }
        private void SystemPromptComboBox2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // SystemPromptComboBox2で選択しているのと同じ内容をInstructionComboBoxにセット
            SystemPromptComboBox.SelectedIndex = SystemPromptComboBox2.SelectedIndex;

            // 選択した内容を元にContentsTextBoxに値を格納
            string selectInstructionContent = "";
            if (!String.IsNullOrEmpty(SystemPromptComboBox2.SelectedItem.ToString()))
            {
                string[] instructionList = AppSettings.InstructionListSetting?.Cast<string>().Where((s, i) => i % 2 == 0).ToArray();
                int index = Array.IndexOf(instructionList, SystemPromptComboBox2.SelectedItem.ToString());
                selectInstructionContent = AppSettings.InstructionListSetting[index, 1];
            }
            SystemPromptContentsTextBox.Text = selectInstructionContent;
            UnsavedLabel.Visibility = Visibility.Collapsed;
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
        private void ConversationHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            ShowTable();
        }
        private void TokenUsage_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var window = new TokenUsageWindow();
            window.Owner = this;
            window.ShowDialog();
        }
        private void ConfigurationSettingButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new ConfigSettingWindow();
            window.Owner = this;
            window.ShowDialog();
            ConfigurationComboBox.ItemsSource = AppSettings.ConfigDataTable.AsEnumerable().Select(x => x.Field<string>("ConfigurationName")).ToList();
        }
        private void InstructionSettingButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new InstructionSettingWindow(AppSettings.InstructionListSetting);
            window.Owner = this;
            bool result = (bool)window.ShowDialog();
            if (result)
            {
                AppSettings.InstructionListSetting = result ? window.inputResult : null;
                //instructionListSettingの1列目を取得
                string[] instructionList = AppSettings.InstructionListSetting?.Cast<string>().Where((s, i) => i % 2 == 0).ToArray();
                Array.Resize(ref instructionList, instructionList.Length + 1);
                instructionList[instructionList.Length - 1] = "";
                SystemPromptComboBox.ItemsSource = instructionList;
                SystemPromptComboBox2.ItemsSource = instructionList;
            }
        }
        private void ColorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var window = new ColorSettings();
            window.Owner = this;
            window.ShowDialog();
        }
        private void TranslationAPIMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var window = new TranslationAPISettingWindow();
            window.Owner = this;
            window.ShowDialog();
        }
        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            //UserTextBox.Width = Math.Max(UserTextBox.ActualWidth + e.HorizontalChange, UserTextBox.MinWidth);
            UserTextBox.Height = Math.Max(UserTextBox.ActualHeight + e.VerticalChange, UserTextBox.MinHeight);
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            // ウィンドウのサイズを保存
            SaveWindowBounds();
            base.OnClosing(e);
        }
        void SaveWindowBounds()
        {
            var settings = Properties.Settings.Default;
            settings.WindowMaximized = WindowState == WindowState.Maximized;
            WindowState = WindowState.Normal; // 最大化解除
            settings.WindowLeft = Left;
            settings.WindowTop = Top;
            settings.WindowWidth = Width;
            settings.WindowHeight = Height;
            if (SystemPromptGridColumn.Width.Value > 0)
            {
                Properties.Settings.Default.SystemPromptColumnWidth = SystemPromptGridColumn.Width.Value;
            }
            if (ConversationHistorytGridColumn.Width.Value > 0)
            {
                Properties.Settings.Default.ConversationColumnWidth = ConversationHistorytGridColumn.Width.Value;
            }
            settings.Save();
        }
        void RecoverWindowBounds()
        {
            var settings = Properties.Settings.Default;
            // 左
            if (settings.WindowLeft >= 0 &&
                (settings.WindowLeft + settings.WindowWidth) < SystemParameters.VirtualScreenWidth)
            { Left = settings.WindowLeft; }
            // 上
            if (settings.WindowTop >= 0 &&
                (settings.WindowTop + settings.WindowHeight) < SystemParameters.VirtualScreenHeight)
            { Top = settings.WindowTop; }
            // 幅
            if (settings.WindowWidth > 0 &&
                settings.WindowWidth <= SystemParameters.WorkArea.Width)
            { Width = settings.WindowWidth; }
            // 高さ
            if (settings.WindowHeight > 0 &&
                settings.WindowHeight <= SystemParameters.WorkArea.Height)
            { Height = settings.WindowHeight; }
            // 最大化
            if (settings.WindowMaximized)
            {
                // ロード後に最大化
                Loaded += (o, e) => WindowState = WindowState.Maximized;
            }
        }
        private void ShowTable()
        {
            ConversationHistory targetConversation = ConversationListBox.SelectedItem as ConversationHistory;
            if (targetConversation == null)
            {
                return;
            }

            var window = new Table(targetConversation);
            window.Owner = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            bool result = (bool)window.ShowDialog();
            if (result)
            {
                targetConversation.Messages = window.UpdatedConversationHistory.Messages;
                SetMessages();
            }
        }
        private void SetMessages()
        {
            MessagesPanel.Children.Clear();

            ConversationHistory selectedConversation = ConversationListBox.SelectedItem as ConversationHistory;
            if (selectedConversation == null)
            {
                return;
            }

            foreach (var message in selectedConversation.Messages)
            {
                if (message.Role == null) { break; }
                bool isUser = message.Role == "user";
                var messageElement = CreateMessageElement(message.Content, isUser);
                MessagesPanel.Children.Add(messageElement);
            }
            MessagesPanel.PreviewMouseWheel += PreviewMouseWheel;
        }
        /// <summary>
        /// メッセージの要素を作成する
        /// </summary>
        private FrameworkElement CreateMessageElement(string messageContent, bool isUser)
        {
            var accentColor = ThemeManager.Current.AccentColor;
            if (accentColor == null)
            {
                accentColor = SystemParameters.WindowGlassColor;
            }
            var accentColorBrush = new SolidColorBrush((Color)accentColor);
            accentColorBrush.Opacity = 0.3;

            Grid messageGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(8, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                }
            };
            // グリッドのサイズが変更されたときにイベントを追加
            messageGrid.SizeChanged += MessageGrid_SizeChanged;

            if (isUser)
            {
                TextBlock userTextBlock = new TextBlock
                {
                    Padding = new Thickness(10),
                    FontSize = Properties.Settings.Default.FontSize,
                    //Background = accentColorBrush,
                    TextAlignment = TextAlignment.Left,
                    TextWrapping = TextWrapping.Wrap,
                    Text = messageContent
                };
                userTextBlock.MouseDown += UserTextBlock_MouseDown;

                ContextMenu contextMenu = CreateContextMenu();
                userTextBlock.ContextMenu = contextMenu;

                Grid.SetColumn(userTextBlock, 1);
                messageGrid.Children.Add(userTextBlock);

                // 行全体の背景色を設定する
                Rectangle backgroundRect = new Rectangle { Fill = accentColorBrush };
                Grid.SetColumnSpan(backgroundRect, 3);
                messageGrid.Children.Add(backgroundRect);
                Panel.SetZIndex(backgroundRect, -1);
            }
            else
            {
                var pipeline = new MarkdownPipelineBuilder()
                .UseSoftlineBreakAsHardlineBreak()
                .UseAdvancedExtensions()
                .Build();

                var flowDocument = Markdig.Wpf.Markdown.ToFlowDocument(messageContent, pipeline);
                var richTextBox = new RichTextBox
                {
                    Padding = new Thickness(5, 10, 5, 10),
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Document = flowDocument
                };
                richTextBox.Document.FontSize = Properties.Settings.Default.FontSize;
                richTextBox.Document.FontFamily = new FontFamily("Yu Gothic UI");

                ContextMenu contextMenu = CreateContextMenu();
                richTextBox.ContextMenu = contextMenu;

                Grid.SetColumn(richTextBox, 1);
                messageGrid.Children.Add(richTextBox);
            }

            return messageGrid;
        }
        private void UserTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            ConversationListBox.Focus(); // ショートカットキーを有効にするためにListBoxにフォーカスを移す
        }
        private void MessageGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Grid messageGrid)
            {
                if (messageGrid.ActualWidth * 0.8 > 1200)
                {
                    messageGrid.ColumnDefinitions[1].Width = new GridLength(1200);
                }
                else
                {
                    messageGrid.ColumnDefinitions[1].Width = new GridLength(messageGrid.ActualWidth * 0.8);
                }
            }
        }
        /// <summary>
        /// 右クリックメニュー
        /// </summary>
        private ContextMenu CreateContextMenu()
        {
            ContextMenu contextMenu = new ContextMenu();

            MenuItem copyTextMenuItem = new MenuItem();
            copyTextMenuItem.Icon = new ModernWpf.Controls.SymbolIcon(ModernWpf.Controls.Symbol.Copy);
            Button copyTextButton = new Button { Content = "Copy Text", Background = Brushes.Transparent };
            Action copyTextAndCloseMenu = () =>
            {
                CopyTextToClipboard(contextMenu.PlacementTarget);
                contextMenu.IsOpen = false;
            };
            copyTextButton.Click += (s, e) => copyTextAndCloseMenu();
            copyTextMenuItem.Click += (s, e) => copyTextAndCloseMenu();
            copyTextMenuItem.Header = copyTextButton;
            void CopyTextToClipboard(object target)
            {
                if (target is TextBlock textBlock)
                {
                    Clipboard.SetText(textBlock.Text);
                }
                else if (target is RichTextBox richTextBox)
                {
                    TextRange textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                    Clipboard.SetText(textRange.Text);
                }
            }
            contextMenu.Items.Add(copyTextMenuItem);

            MenuItem increaseFontSizeMenuItem = new MenuItem();
            increaseFontSizeMenuItem.Icon = new ModernWpf.Controls.SymbolIcon(ModernWpf.Controls.Symbol.FontIncrease);
            Button increaseFontSizeButton = new Button { Content = "Increase Font Size", Background = Brushes.Transparent };
            increaseFontSizeMenuItem.Header = increaseFontSizeButton;
            increaseFontSizeButton.Click += (s, e) => SetFontSize(Properties.Settings.Default.FontSize + 1);
            increaseFontSizeMenuItem.Click += (s, e) => SetFontSize(Properties.Settings.Default.FontSize + 1);
            contextMenu.Items.Add(increaseFontSizeMenuItem);

            MenuItem decreaseFontSizeMenuItem = new MenuItem();
            decreaseFontSizeMenuItem.Icon = new ModernWpf.Controls.SymbolIcon(ModernWpf.Controls.Symbol.FontDecrease);
            Button decreaseFontSizeButton = new Button { Content = "Decrease Font Size", Background = Brushes.Transparent };
            decreaseFontSizeMenuItem.Header = decreaseFontSizeButton;
            decreaseFontSizeButton.Click += (s, e) => SetFontSize(Properties.Settings.Default.FontSize - 1);
            decreaseFontSizeMenuItem.Click += (s, e) => SetFontSize(Properties.Settings.Default.FontSize - 1);
            contextMenu.Items.Add(decreaseFontSizeMenuItem);

            MenuItem defaultFontSizeMenuItem = new MenuItem { Header = "Default Font Size" };
            defaultFontSizeMenuItem.Icon = new ModernWpf.Controls.SymbolIcon(ModernWpf.Controls.Symbol.Refresh);
            Button defaultFontSizeButton = new Button { Content = "Default Font Size", Background = Brushes.Transparent };
            defaultFontSizeMenuItem.Header = defaultFontSizeButton;
            defaultFontSizeButton.Click += (s, e) => SetFontSize(16);
            defaultFontSizeMenuItem.Click += (s, e) => SetFontSize(16);
            contextMenu.Items.Add(defaultFontSizeMenuItem);

            void SetFontSize(int newSize)
            {
                int minSize = 8;
                int maxSize = 32;
                newSize = Math.Max(minSize, Math.Min(maxSize, newSize));

                Properties.Settings.Default.FontSize = newSize;
                Properties.Settings.Default.Save();
                foreach (var item in MessagesPanel.Children)
                {
                    if (item is Grid grid)
                    {
                        foreach (var child in grid.Children)
                        {
                            if (child is TextBlock textBlock)
                            {
                                textBlock.FontSize = newSize;
                            }
                            else if (child is RichTextBox richTextBox)
                            {
                                richTextBox.Document.FontSize = newSize;
                            }
                        }
                    }
                }
            }

            MenuItem translateMenuItem = new MenuItem();
            translateMenuItem.Icon = new ModernWpf.Controls.SymbolIcon(ModernWpf.Controls.Symbol.Globe);
            Button translateButton = new Button { Content = "Translate", Background = Brushes.Transparent };
            Action translateTextAndCloseMenu = () =>
            {
                TranslateText(contextMenu.PlacementTarget);
                contextMenu.IsOpen = false;
            };
            translateButton.Click += (s, e) => translateTextAndCloseMenu();
            translateMenuItem.Click += (s, e) => translateTextAndCloseMenu();
            translateMenuItem.Header = translateButton;
            translateMenuItem.Visibility = AppSettings.TranslationAPIUseFlg ? Visibility.Visible : Visibility.Collapsed;

            async void TranslateText(object target)
            {
                Storyboard? animation = null;
                if (target is TextBlock textBlock)
                {
                    try
                    {
                        animation = CreateOpacityAnimation(textBlock);
                        animation.Begin();

                        string text = textBlock.Text;
                        string text2 = await TranslateAPIRequestAsync(text, AppSettings.FromTranslationLanguage);

                        text2 = text2.TrimEnd('\r', '\n');
                        textBlock.Text = text2;
                    }
                    catch (Exception ex)
                    {
                        ModernWpf.MessageBox.Show(ex.Message);
                    }
                    finally
                    {
                        animation?.Stop();
                        textBlock.Opacity = 1.0;
                    }
                }
                else if (target is RichTextBox richTextBox)
                {
                    try
                    {
                        animation = CreateOpacityAnimation(richTextBox);
                        animation.Begin();

                        // 元のRichTextBoxのデータ(ListItem、Paragraph、Text)を保存するリスト
                        List<(ListItem listItem, Paragraph paragraph, string text)> originalData = new List<(ListItem listItem, Paragraph paragraph, string text)>();
                        foreach (Block block in richTextBox.Document.Blocks)
                        {
                            // 各ブロックを処理して、originalDataリストにデータを追加
                            ProcessBlocks(new List<Block> { block }, originalData);
                        }

                        // 元のデータの各アイテムを翻訳
                        foreach (var (listItem, paragraph, text) in originalData)
                        {
                            string translatedText = await TranslateAPIRequestAsync(text, AppSettings.FromTranslationLanguage);
                            translatedText = translatedText.TrimEnd('\r', '\n');

                            // パラグラフが存在する場合、翻訳されたテキストで更新
                            if (paragraph != null)
                            {
                                paragraph.Inlines.Clear();
                                paragraph.Inlines.Add(new Run(translatedText));
                            }
                            // リストアイテムが存在する場合、翻訳されたテキストで更新
                            else if (listItem != null && listItem.Blocks.FirstBlock is Paragraph listItemParagraph)
                            {
                                listItemParagraph.Inlines.Clear();
                                listItemParagraph.Inlines.Add(new Run(translatedText));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ModernWpf.MessageBox.Show(ex.Message);
                    }
                    finally
                    {
                        animation?.Stop();
                        richTextBox.Opacity = 1.0;
                    }
                }
            }
            void ProcessBlocks(IEnumerable<Block> blocks, List<(ListItem listItem, Paragraph paragraph, string text)> originalData)
            {
                foreach (Block block in blocks)
                {
                    // ブロックが独立した段落(リストアイテムの一部ではない)であるかを確認
                    if (block is Paragraph paragraph && !(block.Parent is ListItem))
                    {
                        string paragraphText = new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text.Trim();
                        originalData.Add((null, paragraph, paragraphText));
                    }
                    else if (block is List list)
                    {
                        foreach (ListItem listItem in list.ListItems)
                        {
                            // リストアイテムの最初のブロックが段落であるかを確認
                            if (listItem.Blocks.FirstBlock is Paragraph listItemParagraph)
                            {
                                string listItemText = new TextRange(listItemParagraph.ContentStart, listItemParagraph.ContentEnd).Text.Trim();
                                // リストアイテムのテキストから番号または箇条書きを削除
                                var match = Regex.Match(listItemText, @"^(\d+\.\s+|•\s+)");
                                if (match.Success)
                                {
                                    listItemText = listItemText.Substring(match.Length);
                                }
                                originalData.Add((listItem, null, listItemText));
                            }
                            // リストアイテム内のブロックを再帰的に処理
                            ProcessBlocks(listItem.Blocks, originalData);
                        }
                    }
                    else if (block is Section section)
                    {
                        ProcessBlocks(section.Blocks, originalData);
                    }
                }
            }
            contextMenu.Items.Add(translateMenuItem);

            return contextMenu;
        }
        /// <summary>
        /// 親のScrollViewerでスクロールする
        /// </summary>
        private void PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            UIElement element = sender as UIElement;
            // 親要素を辿るループ
            while (element != null)
            {
                // 親要素を取得し、UIElementとしてelementに代入
                element = VisualTreeHelper.GetParent(element) as UIElement;
                // elementがScrollViewer型であるかどうかをチェック
                if (element is ScrollViewer scrollViewer)
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - (e.Delta / 3));
                    e.Handled = true;
                    return;
                }
            }
        }
        private void UseConversationHistoryToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (UseConversationHistoryToggleSwitch.IsOn == false)
            {
                AppSettings.UseConversationHistoryFlg = false;
            }
            else
            {
                AppSettings.UseConversationHistoryFlg = true;
            }
        }
        private void ConversationHistoryClearButton_Click(object sender, RoutedEventArgs e)
        {
            var yesno = ModernWpf.MessageBox.Show("Do you want to delete the entire conversation history?", "Delete Conversation History", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (yesno == MessageBoxResult.No)
            {
                return;
            }

            ConversationHistory targetConversation = ConversationListBox.SelectedItem as ConversationHistory;
            if (targetConversation == null)
            {
                return;
            }
            targetConversation.Messages.Clear();

            //MessagesPanelをすべてクリア
            MessagesPanel.Children.Clear();
        }
        private void MessageScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // スクロールが一番下にあるかどうかをチェック
            bool isAtBottom = MessageScrollViewer.VerticalOffset >= MessageScrollViewer.ScrollableHeight;
            // ボタンの表示/非表示を切り替え  
            BottomScrollButton.Visibility = isAtBottom ? Visibility.Collapsed : Visibility.Visible;
        }
        private void BottomScrollButton_Click(object sender, RoutedEventArgs e)
        {
            MessageScrollViewer.ScrollToBottom();
        }
        private void MessageScrollViewer_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.G && Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                MessageScrollViewer.ScrollToBottom();
                gKeyPressed = false;
            }
            else if (e.Key == Key.G)
            {
                if (gKeyPressed)
                {
                    MessageScrollViewer.ScrollToTop();
                    gKeyPressed = false;
                }
                else
                {
                    gKeyPressed = true;
                }
            }
            else if (e.Key == Key.U && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                double newVerticalOffset = MessageScrollViewer.VerticalOffset - (MessageScrollViewer.ViewportHeight / 2);
                MessageScrollViewer.ScrollToVerticalOffset(newVerticalOffset);
                gKeyPressed = false;
            }
            else if (e.Key == Key.D && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                double newVerticalOffset = MessageScrollViewer.VerticalOffset + (MessageScrollViewer.ViewportHeight / 2);
                MessageScrollViewer.ScrollToVerticalOffset(newVerticalOffset);
                gKeyPressed = false;
            }
            else if (e.Key == Key.E && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                double newVerticalOffset = MessageScrollViewer.VerticalOffset + 20;
                MessageScrollViewer.ScrollToVerticalOffset(newVerticalOffset);
                gKeyPressed = false;
            }
            else if (e.Key == Key.Y && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                double newVerticalOffset = MessageScrollViewer.VerticalOffset - 20;
                MessageScrollViewer.ScrollToVerticalOffset(newVerticalOffset);
                gKeyPressed = false;
            }
            else if (e.Key == Key.J)
            {
                double newVerticalOffset = MessageScrollViewer.VerticalOffset + 20;
                MessageScrollViewer.ScrollToVerticalOffset(newVerticalOffset);
                gKeyPressed = false;
            }
            else if (e.Key == Key.K)
            {
                double newVerticalOffset = MessageScrollViewer.VerticalOffset - 20;
                MessageScrollViewer.ScrollToVerticalOffset(newVerticalOffset);
                gKeyPressed = false;
            }
            else
            {
                gKeyPressed = false;
            }
        }
        private void InitializeSystemPromptColumn()
        {
            if (AppSettings.IsSystemPromptColumnVisible == true)
            {
                SystemPromptGridColumn.Width = new GridLength(Properties.Settings.Default.SystemPromptColumnWidth);
                GridSplitterGridColumn.Width = new GridLength(1, GridUnitType.Auto);
                SystemPromptSplitter.Visibility = Visibility.Visible;
                OpenSytemPromptWindowButtonIcon.Symbol = ModernWpf.Controls.Symbol.ClosePane;
                // InstructionComboBoxで選択しているのと同じ内容をSystemPromptComboBox2にセット
                SystemPromptComboBox2.SelectedIndex = SystemPromptComboBox.SelectedIndex;
            }
            else
            {
                SystemPromptGridColumn.Width = new GridLength(0);
                GridSplitterGridColumn.Width = new GridLength(0);
                SystemPromptSplitter.Visibility = Visibility.Hidden;
                OpenSytemPromptWindowButtonIcon.Symbol = ModernWpf.Controls.Symbol.OpenPane;
            }
            if (AppSettings.IsConversationColumnVisible == true)
            {
                ConversationHistorytGridColumn.Width = new GridLength(Properties.Settings.Default.ConversationColumnWidth);
                GridSplitterGridColumn2.Width = new GridLength(1, GridUnitType.Auto);
            }
            else
            {
                ConversationHistorytGridColumn.Width = new GridLength(0);
                GridSplitterGridColumn2.Width = new GridLength(0);
            }

            //var accentColor = ThemeManager.Current.AccentColor;
            //if (accentColor == null)
            //{
            //    accentColor = SystemParameters.WindowGlassColor;
            //}
            //var accentColorBrush = new SolidColorBrush((Color)accentColor);
            //accentColorBrush.Opacity = 0.08;
            //SystemPromptGrid.Background = accentColorBrush;
        }
        private void OpenSytemPromptWindowButton_Click(object sender, RoutedEventArgs e)
        {
            if (SystemPromptGridColumn.Width.Value > 0)
            {
                Properties.Settings.Default.SystemPromptColumnWidth = SystemPromptGridColumn.Width.Value;
                Properties.Settings.Default.Save();
                SystemPromptGridColumn.Width = new GridLength(0);
                GridSplitterGridColumn.Width = new GridLength(0);
                SystemPromptSplitter.Visibility = Visibility.Hidden;
                OpenSytemPromptWindowButtonIcon.Symbol = ModernWpf.Controls.Symbol.OpenPane;
                AppSettings.IsSystemPromptColumnVisible = false;
            }
            else
            {
                SystemPromptGridColumn.Width = new GridLength(Properties.Settings.Default.SystemPromptColumnWidth);
                GridSplitterGridColumn.Width = new GridLength(1, GridUnitType.Auto);
                SystemPromptSplitter.Visibility = Visibility.Visible;
                OpenSytemPromptWindowButtonIcon.Symbol = ModernWpf.Controls.Symbol.ClosePane;
                AppSettings.IsSystemPromptColumnVisible = true;
                // InstructionComboBoxで選択しているのと同じ内容をSystemPromptComboBox2にセット
                SystemPromptComboBox2.SelectedIndex = SystemPromptComboBox.SelectedIndex;
            }
            if (AppSettings.IsConversationColumnVisible == true)
            {
                ConversationHistorytGridColumn.Width = new GridLength(Properties.Settings.Default.ConversationColumnWidth);
                GridSplitterGridColumn2.Width = new GridLength(1, GridUnitType.Auto);
            }
            else
            {
                ConversationHistorytGridColumn.Width = new GridLength(0);
                GridSplitterGridColumn2.Width = new GridLength(0);
            }
        }

        private void SystemPromptContentsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UnsavedLabel.Visibility = Visibility.Visible;
        }
        private void NewChatButton_Click(object sender, RoutedEventArgs e)
        {
            MessagesPanel.Children.Clear();
            
            if (ConversationListBox.SelectedItem is ConversationHistory selectedItem)
            {
                selectedItem.IsSelected = false;
            }
            ConversationListBox.SelectedItem = null;

            UserTextBox.Focus();
            UserTextBox.CaretIndex = UserTextBox.Text.Length;
        }
        private void ConversationDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            ConversationHistory itemToDelete = (ConversationHistory)((Button)sender).DataContext;
            var result = ModernWpf.MessageBox.Show("Are you sure you want to delete this conversation?",
                                                   "Confirmation",
                                                   MessageBoxButton.YesNo,
                                                   MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                AppSettings.ConversationManager.Histories.Remove(itemToDelete);
                ConversationListBox.Items.Refresh();
            }
        }
        private void ConversationTitleEditButton_Click(object sender, RoutedEventArgs e)
        {
            ConversationHistory itemToDelete = (ConversationHistory)((Button)sender).DataContext;
            string currentTitle = itemToDelete.Title;

            var editWindow = new TitleEditWindow(currentTitle);
            editWindow.Owner = this;

            if (editWindow.ShowDialog() == true)
            {
                string newTitle = editWindow.NewTitle;
                itemToDelete.Title = newTitle;
            }
        }
        private void ConversationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ConversationListBox.SelectedItem == null)
            {
                MessagesPanel.Children.Clear();
                return;
            }
            var selectedConversation = (ConversationHistory)ConversationListBox.SelectedItem;
            List<ChatMessage> messages = selectedConversation.Messages.ToList();

            MessagesPanel.Children.Clear();

            foreach (var message in messages)
            {
                if (message.Role == null) { break; }
                bool isUser = message.Role == "user";
                var messageElement = CreateMessageElement(message.Content, isUser);
                MessagesPanel.Children.Add(messageElement);
            }

            MessagesPanel.PreviewMouseWheel += PreviewMouseWheel;

            // 削除ボタン活性制御用
            foreach (var item in ConversationListBox.Items.OfType<ConversationHistory>())
            {
                item.IsSelected = false;
            }        
            if (selectedConversation != null)
            {
                selectedConversation.IsSelected = true;
            }
            foreach (ConversationHistory item in e.RemovedItems)
            {
                item.IsSelected = false;
            }

            UserTextBox.Focus();
            UserTextBox.CaretIndex = UserTextBox.Text.Length;
        }
        public void RefreshConversationList()
        {
            var collectionViewSource = FindResource("SortedConversations") as CollectionViewSource;
            if (collectionViewSource != null)
            {
                collectionViewSource.Source = AppSettings.ConversationManager.Histories;
                collectionViewSource.View.Refresh();
            }
        }
        private void ToastNotificationManagerCompat_OnActivated(ToastNotificationActivatedEventArgsCompat e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.Activate();
                this.Topmost = true;
                this.Topmost = false;
            });
        }
        private async void TranslateButton_Click(object sender, RoutedEventArgs e)
        {
            Storyboard? animation = null;
            Color initialTextColor;
            try
            {
                TranslateButton.IsEnabled = false;
                TranslationProgressRing.IsActive = true;
                animation = CreateTextColorAnimation(UserTextBox, out initialTextColor);
                animation.Begin();

                string resultText = await TranslateAPIRequestAsync(UserTextBox.Text, AppSettings.ToTranslationLanguage);
                UserTextBox.Text = resultText;
                UserTextBox.CaretIndex = UserTextBox.Text.Length;
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                TranslateButton.IsEnabled = true;
                TranslationProgressRing.IsActive = false;
                animation?.Stop();
                UserTextBox.Foreground = new SolidColorBrush(initialTextColor);  
            }
        }
    }
}
