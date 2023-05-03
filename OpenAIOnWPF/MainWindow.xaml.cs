using ModernWpf;
using Newtonsoft.Json;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.Tokenizer.GPT3;
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
using System.Windows.Input;
using System.Windows.Input.Manipulations;
using System.Windows.Media;
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

        public MainWindow()
        {
            InitializeComponent();
            RecoverWindowBounds();
            InitializeSettings();
            setMessages();
        }
        private void InitializeSettings()
        {
            InitialColorSet();
            UserTextBox.Focus();
            NoticeCheckbox.IsChecked = AppSettings.NoticeFlgSetting;

            // Settingsから指示内容リストを取得しセット
            InstructionComboBox.ItemsSource = SetupInstructionComboBox();
            InstructionComboBox.Text = String.IsNullOrEmpty(AppSettings.InstructionSetting) ? "" : AppSettings.InstructionSetting;

            var appSettings = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal);
            Debug.Print("Path to save the configuration file:" + appSettings.FilePath);

            InitializeConfigDataTable();

            ConfigurationComboBox.ItemsSource = AppSettings.ConfigDataTable.AsEnumerable().Select(x => x.Field<string>("ConfigurationName")).ToList();
            ConfigurationComboBox.Text = AppSettings.SelectConfigSetting;

            UseConversationHistoryCheckBox.IsChecked = AppSettings.UseConversationHistoryFlg;

            MessageScrollViewer.ScrollToBottom();
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
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
        private void NoticeCheckbox_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.NoticeFlgSetting = (bool)NoticeCheckbox.IsChecked;
        }

        private void TokensLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShowMessagebox("Tokens",TokensLabel.ToolTip.ToString());
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
            if (InstructionComboBox.SelectedItem == null) return;
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
        private void setMessages()
        {
            var accentColor = ThemeManager.Current.AccentColor;
            if (accentColor == null)
            {
                accentColor = SystemParameters.WindowGlassColor;
            }
            var accentColorBrush = new SolidColorBrush((Color)accentColor);
            //var accentColorBrush =  (Brush)Application.Current.Resources["SystemChromeDisabledHighColorBrush"];
            accentColorBrush.Opacity = 0.5;

            AppSettings.ConversationHistory = JsonConvert.DeserializeObject<List<ChatMessage>>(Properties.Settings.Default.ConversationHistory);

            if (AppSettings.ConversationHistory != null)
            {
                foreach (var message in AppSettings.ConversationHistory)
                {
                    TextBlock usermarkdownScrollViewer = new TextBlock();
                    MdXaml.MarkdownScrollViewer markdownScrollViewer = new MdXaml.MarkdownScrollViewer();
                    if (message.Role == "user")
                    {
                        usermarkdownScrollViewer.Padding = new Thickness(100, 10, 100, 10);
                        usermarkdownScrollViewer.FontSize = 16;
                        usermarkdownScrollViewer.Background = accentColorBrush;
                        MessagesPanel.Children.Add(usermarkdownScrollViewer);
                        MessagesPanel.PreviewMouseWheel += PreviewMouseWheel;
                        usermarkdownScrollViewer.Text = message.Content;
                    }
                    else if (message.Role == "assistant")
                    {
                        markdownScrollViewer.Padding = new Thickness(100, 10, 100, 10);
                        markdownScrollViewer.FontSize = 14;
                        markdownScrollViewer.MarkdownStyleName = "Sasabune";
                        markdownScrollViewer.MouseWheel += AssistantMarkdownText_MouseWheel;
                        MessagesPanel.Children.Add(markdownScrollViewer);
                        MessagesPanel.PreviewMouseWheel += PreviewMouseWheel;
                        markdownScrollViewer.Markdown = message.Content.Replace("\r\n","  \r\n");
                    }
                    ContextMenu contextMenu = CreateFontSizeContextMenu(usermarkdownScrollViewer , markdownScrollViewer);
                    markdownScrollViewer.ContextMenu = contextMenu;
                    usermarkdownScrollViewer.ContextMenu = contextMenu;
                }
            }
        }
        /// <summary>
        /// フォントサイズを変更する右クリックメニュー
        /// </summary>
        public ContextMenu CreateFontSizeContextMenu(TextBlock textBlock, MdXaml.MarkdownScrollViewer markdownScrollViewer)
        {
            ContextMenu contextMenu = new ContextMenu();

            MenuItem fontSizeSmall = new MenuItem { Header = "Small Font" };
            fontSizeSmall.Click += (s, e) => ChangeFontSize(textBlock, markdownScrollViewer, 12);
            contextMenu.Items.Add(fontSizeSmall);

            MenuItem fontSizeMedium = new MenuItem { Header = "Medium Font" };
            fontSizeMedium.Click += (s, e) => ChangeFontSize(textBlock, markdownScrollViewer, 14);
            contextMenu.Items.Add(fontSizeMedium);

            MenuItem fontSizeLarge = new MenuItem { Header = "Large Font" };
            fontSizeLarge.Click += (s, e) => ChangeFontSize(textBlock, markdownScrollViewer, 18);
            contextMenu.Items.Add(fontSizeLarge);

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
        private void UseConversationHistoryCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (UseConversationHistoryCheckBox.IsChecked == false)
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
    }
}
