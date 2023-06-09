﻿using Markdig;
using Markdig.Wpf;
using ModernWpf;
using Newtonsoft.Json;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.Tokenizer.GPT3;
using SourceChord.FluentWPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Input.Manipulations;
using System.Windows.Media;
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
        string userMessage = "";
        Stopwatch stopWatch = new Stopwatch();
        private bool gKeyPressed;

        public MainWindow()
        {
            InitializeComponent();
            RecoverWindowBounds();
            InitializeSettings();
            SetMessages();
        }
        private void InitializeSettings()
        {
            InitialColorSet();
            UserTextBox.Focus();
            NoticeToggleSwitch.IsOn = AppSettings.NoticeFlgSetting;

            // Settingsから指示内容リストを取得しセット
            InstructionComboBox.ItemsSource = SetupInstructionComboBox();
            InstructionComboBox.Text = String.IsNullOrEmpty(AppSettings.InstructionSetting) ? "" : AppSettings.InstructionSetting;
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
        private void InstructionComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // SystemPromptComboBox2で選択しているのと同じ内容をInstructionComboBoxにセット
            SystemPromptComboBox2.SelectedIndex = InstructionComboBox.SelectedIndex;
            if (InstructionComboBox.SelectedItem == "")
            {
                AppSettings.InstructionSetting = "";
                return;
            }
            AppSettings.InstructionSetting = InstructionComboBox.SelectedItem.ToString();
            // ツールチップに内容を表示
            string selectInstructionContent = "";
            if (!String.IsNullOrEmpty(AppSettings.InstructionSetting))
            {
                string[] instructionList = AppSettings.InstructionListSetting?.Cast<string>().Where((s, i) => i % 2 == 0).ToArray();
                int index = Array.IndexOf(instructionList, AppSettings.InstructionSetting);
                selectInstructionContent = AppSettings.InstructionListSetting[index, 1];
            }
            InstructionComboBox.ToolTip = "# " + AppSettings.InstructionSetting + "\r\n"
                                            + selectInstructionContent;
        }
        private void SystemPromptComboBox2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // SystemPromptComboBox2で選択しているのと同じ内容をInstructionComboBoxにセット
            InstructionComboBox.SelectedIndex = SystemPromptComboBox2.SelectedIndex;

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
                InstructionComboBox.ItemsSource = instructionList;
            }
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
            if (AppSettings.ConversationHistory == null)
            {
                AppSettings.ConversationHistory = new List<ChatMessage>();
            }
            int count = AppSettings.ConversationHistory.Count;
            string[,] table = new string[count, 2];
            foreach (var item in AppSettings.ConversationHistory)
            {
                table[AppSettings.ConversationHistory.IndexOf(item), 0] = item.Role;
                table[AppSettings.ConversationHistory.IndexOf(item), 1] = item.Content;
            }
            var window = new Table(table);
            window.Owner = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            bool result = (bool)window.ShowDialog();
            if (result)
            {
                SetMessages();
            }
        }
        private void SetMessages()
        {
            MessagesPanel.Children.Clear();

            AppSettings.ConversationHistory = JsonConvert.DeserializeObject<List<ChatMessage>>(Properties.Settings.Default.ConversationHistory);

            if (AppSettings.ConversationHistory != null)
            {
                foreach (var message in AppSettings.ConversationHistory)
                {
                    if (message.Role == null) { break; }
                    bool isUser = message.Role == "user";
                    var messageElement = CreateMessageElement(message.Content, isUser);
                    MessagesPanel.Children.Add(messageElement);
                }
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
                    FontSize = 16,
                    //Background = accentColorBrush,
                    TextAlignment = TextAlignment.Left,
                    TextWrapping = TextWrapping.Wrap,
                    Text = messageContent
                };

                ContextMenu contextMenu = CreateFontSizeContextMenu();
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
                    Padding = new Thickness(5,10,5,10),
                    FontSize = 16,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Document = flowDocument
                };

                ContextMenu contextMenu = CreateFontSizeContextMenu();
                richTextBox.ContextMenu = contextMenu;

                Grid.SetColumn(richTextBox, 1);
                messageGrid.Children.Add(richTextBox);
            }

            return messageGrid;
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
        /// フォントサイズを変更する右クリックメニュー
        /// </summary>
        private ContextMenu CreateFontSizeContextMenu()
        {
            ContextMenu contextMenu = new ContextMenu();

            MenuItem fontSizeSmall = new MenuItem { Header = "Small Font" };
            fontSizeSmall.Icon = new ModernWpf.Controls.SymbolIcon(ModernWpf.Controls.Symbol.FontDecrease);
            fontSizeSmall.Click += (s, e) => ChangeFontSize(14);
            contextMenu.Items.Add(fontSizeSmall);

            MenuItem fontSizeMedium = new MenuItem { Header = "Medium Font" };
            fontSizeMedium.Icon = new ModernWpf.Controls.SymbolIcon(ModernWpf.Controls.Symbol.FontSize);
            fontSizeMedium.Click += (s, e) => ChangeFontSize(16);
            contextMenu.Items.Add(fontSizeMedium);

            MenuItem fontSizeLarge = new MenuItem { Header = "Large Font" };
            fontSizeLarge.Icon = new ModernWpf.Controls.SymbolIcon(ModernWpf.Controls.Symbol.FontIncrease);
            fontSizeLarge.Click += (s, e) => ChangeFontSize(18);
            contextMenu.Items.Add(fontSizeLarge);

            // MessagesPanel配下のオブジェクトのフォントサイズを変更する
            void ChangeFontSize(int fontSize)
            {
                foreach (var item in MessagesPanel.Children)
                {
                    if (item is Grid grid)
                    {
                        foreach (var child in grid.Children)
                        {
                            if (child is TextBlock textBlock)
                            {
                                textBlock.FontSize = fontSize;
                            }
                            //else if (child is MdXaml.MarkdownScrollViewer markdownScrollViewer)
                            else if (child is RichTextBox richTextBox)
                            {
                                richTextBox.Document.FontSize = fontSize;
                                //markdownScrollViewer.FontSize = fontSize;
                            }
                        }
                    }
                }
            }

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
            if (AppSettings.ConversationHistory == null)
            {
                return;
            }
            //AppSettings.ConversationHistoryをすべてクリア
            AppSettings.ConversationHistory.RemoveAll(x => true);
            Properties.Settings.Default.ConversationHistory = "";
            Properties.Settings.Default.Save();

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
                OpenSytemPromptWindowButtonIcon.Symbol = ModernWpf.Controls.Symbol.ClosePane;
                // InstructionComboBoxで選択しているのと同じ内容をSystemPromptComboBox2にセット
                SystemPromptComboBox2.SelectedIndex = InstructionComboBox.SelectedIndex;
                //InstructionComboBox.IsEnabled = false;
            }
            else
            {
                SystemPromptGridColumn.Width = new GridLength(0);
                GridSplitterGridColumn.Width = new GridLength(0);
                OpenSytemPromptWindowButtonIcon.Symbol = ModernWpf.Controls.Symbol.OpenPane;
                //InstructionComboBox.IsEnabled = true;
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
                OpenSytemPromptWindowButtonIcon.Symbol = ModernWpf.Controls.Symbol.OpenPane;
                AppSettings.IsSystemPromptColumnVisible = false;
            }
            else
            {
                SystemPromptGridColumn.Width = new GridLength(Properties.Settings.Default.SystemPromptColumnWidth);
                GridSplitterGridColumn.Width = new GridLength(1, GridUnitType.Auto);
                OpenSytemPromptWindowButtonIcon.Symbol = ModernWpf.Controls.Symbol.ClosePane;
                AppSettings.IsSystemPromptColumnVisible = true;
                // InstructionComboBoxで選択しているのと同じ内容をSystemPromptComboBox2にセット
                SystemPromptComboBox2.SelectedIndex = InstructionComboBox.SelectedIndex;
            }
        }

        private void SystemPromptContentsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UnsavedLabel.Visibility = Visibility.Visible;
        }
    }
}
