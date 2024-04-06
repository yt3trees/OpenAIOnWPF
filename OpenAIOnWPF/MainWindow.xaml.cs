using Markdig;
using Markdig.Wpf;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using ModernWpf;
using ModernWpf.Controls;
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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Input.Manipulations;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
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
        private bool isFiltering = false;
        public string? imageFilePath = null;
        public bool visionEnabled = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeSettings();
            RecoverWindowBounds();
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var collectionViewSource = FindResource("SortedConversations") as CollectionViewSource;
            if (collectionViewSource != null)
            {
                collectionViewSource.Source = AppSettings.ConversationManager.Histories;
                ConversationListBox.ItemsSource = collectionViewSource.View;
            }
            var promptTemplateSource = FindResource("SortedPromptTemplates") as CollectionViewSource;
            if (promptTemplateSource != null)
            {
                promptTemplateSource.Source = AppSettings.PromptTemplateManager.Templates;
                PromptTemplateListBox.ItemsSource = promptTemplateSource.View;
            }
            PromptTemplateListBox.SelectedItem = null;

            if (AppSettings.PromptTemplateGridRowHeighSetting > 0)
            {
                ChatListGridRow.Height = new GridLength(AppSettings.ChatListGridRowHeightSetting, GridUnitType.Star);
                PromptTemplateGridRow.Height = new GridLength(AppSettings.PromptTemplateGridRowHeighSetting, GridUnitType.Star);
            }
            else
            {
                PromptTemplateGridRow.Height = new GridLength(0);
            }
        }
        private void InitializeSettings()
        {
            // 前バージョンの設定を引き継ぐ
            if (Properties.Settings.Default.UpgradeRequired)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
                Properties.Settings.Default.Save();
            }

            InitialColorSet();
            ToastNotificationManagerCompat.OnActivated += this.ToastNotificationManagerCompat_OnActivated;
            UserTextBox.Focus();
            NoticeToggleSwitch.IsOn = AppSettings.NoticeFlgSetting;

            try
            {
                AppSettings.ConversationManager = LoadConversationsFromJson();
                AppSettings.PromptTemplateManager = LoadPromptTemplateFromJson();
            }
            catch (Exception ex)
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var message = new StringBuilder()
                    .AppendLine("Failed to load conversation history.")
                    .AppendLine(ex.Message)
                    .AppendLine()
                    .AppendLine("Do you want to reset the conversation history?")
                    .AppendLine("If you choose No, the application will exit at this point.")
                    .AppendLine("Please re-launch the application after the problem with the folder where the conversation history is saved has been resolved.")
                    .AppendLine()
                    .AppendLine($"{documentsPath}\\OpenAIOnWPF\\ConversationHistory")
                    .ToString();
                var result = ModernWpf.MessageBox.Show(
                    message, 
                    "Error", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Error
                );
                if (result == MessageBoxResult.Yes)
                {
                    AppSettings.ConversationManager = new ConversationManager();
                }
                else
                {
                      Environment.Exit(1);
                }
            }
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
            if (AppSettings.PromptTemplateManager.Templates == null)
            {
                AppSettings.PromptTemplateManager.Templates = new ObservableCollection<PromptTemplate>();
            }

            // Settingsから指示内容リストを取得しセット
            SystemPromptComboBox.ItemsSource = SetupInstructionComboBox();
            SystemPromptComboBox.Text = String.IsNullOrEmpty(AppSettings.InstructionSetting) ? "" : AppSettings.InstructionSetting;
            SystemPromptComboBox2.ItemsSource = SetupInstructionComboBox();
            SystemPromptComboBox2.Text = String.IsNullOrEmpty(AppSettings.InstructionSetting) ? "" : AppSettings.InstructionSetting;

            var appSettings = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal);
            Debug.Print("Path to save the configuration file:" + appSettings.FilePath);

            InitializeConfigDataTable();
            EnsureColumnsForType(AppSettings.ConfigDataTable, typeof(ConfigSettingWindow.ModelList));
            ConfigurationComboBox.ItemsSource = AppSettings.ConfigDataTable.AsEnumerable().Select(x => x.Field<string>("ConfigurationName")).ToList();
            ConfigurationComboBox.Text = AppSettings.SelectConfigSetting;

            UseConversationHistoryToggleSwitch.IsOn = AppSettings.UseConversationHistoryFlg;

            MessageScrollViewer.ScrollToBottom();

            InitializeSystemPromptColumn();

            bool isCollapsed = !(AppSettings.IsPromptTemplateListVisible);
            PromptTemplateListBox.Visibility = isCollapsed ? Visibility.Collapsed : Visibility.Visible;
            NewTemplateButton.Visibility = isCollapsed ? Visibility.Collapsed : Visibility.Visible;
            ToggleVisibilityPromptTemplateButton.Content = isCollapsed ? "▲" : "▼";

            var currentPadding = UserTextBox.Padding;
            if (AppSettings.TranslationAPIUseFlg == true)
            {
                TranslateButton.Visibility = Visibility.Visible;
                UserTextBox.Padding = new Thickness(currentPadding.Left, currentPadding.Top, 30, currentPadding.Bottom);
            }
            else
            {
                TranslateButton.Visibility = Visibility.Collapsed;
                UserTextBox.Padding = new Thickness(currentPadding.Left, currentPadding.Top, 10, currentPadding.Bottom);
            }

            if (ThemeManager.Current.ActualApplicationTheme == ModernWpf.ApplicationTheme.Dark)
            {
                ConversationListBox.Opacity = 0.9;
                PromptTemplateListBox.Opacity = 0.9;
            }

            ImageFilePathLabel.Content = string.Empty;
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
            {
                string currentText = UserTextBox.Text;
                var window = new LargeUserTextInput(currentText);
                window.Owner = this;
                window.ShowDialog();
                UserTextBox.Focus();
            }
            if (e.Key == Key.F3)
            {
                ShowTable();
            }
        }
        private void AcrylicWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
            {
                NewChatButton_Click(sender, e);
            }
            else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                try
                {
                    SaveConversationsAsJson(AppSettings.ConversationManager);
                    string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    ModernWpf.MessageBox.Show("Saved to " + documentsPath + @"\OpenAIOnWPF\ConversationHistory", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    e.Handled = true;
                }
                catch
                (Exception ex)
                {
                    ModernWpf.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            // Ctrl+Tab で次のアイテムを選択
            else if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (ConversationListBox.SelectedIndex < ConversationListBox.Items.Count - 1)
                {
                    ConversationListBox.SelectedIndex++;
                }
                else
                {
                    ConversationListBox.SelectedIndex = 0;
                }
                ConversationListBox.ScrollIntoView(ConversationListBox.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Tab && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                if (ConversationListBox.SelectedIndex > 0)
                {
                    ConversationListBox.SelectedIndex--;
                }
                else
                {
                    ConversationListBox.SelectedIndex = ConversationListBox.Items.Count - 1;
                }
                ConversationListBox.ScrollIntoView(ConversationListBox.SelectedItem);
                e.Handled = true;
            }
        }
        private void UserTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                _ = ProcessOpenAIAsync(UserTextBox.Text);
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
            if (!(string.IsNullOrWhiteSpace(UserTextBox.Text)))
            {
                _ = ProcessOpenAIAsync(UserTextBox.Text);
            }
        }
        private CancellationTokenSource _cancellationTokenSource;
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }
        private void AssistantMessageGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (isProcessing)
            {
                CancelButton.Visibility = Visibility.Visible;
            }
        }
        private void AssistantMessageGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            CancelButton.Visibility = Visibility.Collapsed;
        }
        private void UserTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // トークン量を表示
            var tokens = TokenizerGpt3.Encode(UserTextBox.Text);
            string tooltip = $"Tokens : {tokens.Count()}";
            UserTextBox.ToolTip = tooltip;
        }
        private void UserTextBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (UserTextBox.ActualHeight >= UserTextBox.MaxHeight)
            {
                ShowLargeTextInputWindowButton.Visibility = Visibility.Visible;
            }
            else
            {
                ShowLargeTextInputWindowButton.Visibility = Visibility.Collapsed;
            }
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
            AppSettings.PromptTemplateGridRowHeighSetting = PromptTemplateGridRow.ActualHeight;
            AppSettings.ChatListGridRowHeightSetting = ChatListGridRow.ActualHeight;
            SaveSettings();
        }
        private void ConfigurationComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ConfigurationComboBox.SelectedItem == null) return;
            AppSettings.SelectConfigSetting = ConfigurationComboBox.SelectedItem.ToString();
            UpdateUIBasedOnVision();
        }
        private void UpdateUIBasedOnVision()
        {
            if (ConfigurationComboBox.SelectedItem == null)
            {
                return;
            }
            string selectedConfigName = ConfigurationComboBox.SelectedItem.ToString();
            // ConfigDataTableから選択された設定名に対応する行を検索
            var row = AppSettings.ConfigDataTable.AsEnumerable()
                        .FirstOrDefault(x => x.Field<string>("ConfigurationName") == selectedConfigName);
            // 対応する行が見つかった場合、その行のVision列の値を取得
            if (row != null)
            {
                visionEnabled = row.Field<bool>("Vision");
            }
            // Visionの値に基づいてAttachFileButtonの表示/非表示を設定
            AttachFileButton.Visibility = visionEnabled ? Visibility.Visible : Visibility.Collapsed;
            var currentPadding = UserTextBox.Padding;
            int leftPadding = visionEnabled ? 35 : 10;
            UserTextBox.Padding = new Thickness(leftPadding, currentPadding.Top, currentPadding.Right, currentPadding.Bottom);
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
                if (e.Delta > 0 && UserTextBox.FontSize < 40)
                {
                    UserTextBox.FontSize += 2;
                }
                else if (e.Delta < 0 && UserTextBox.FontSize > 10)
                {
                    UserTextBox.FontSize -= 2;
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
            UpdateUIBasedOnVision();
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
        private void VersionInformationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var window = new VersionWindow();
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

            var targetMessages = selectedConversation.Messages;
            for (int i = 0; i < targetMessages.Count; i++)
            {
                var message = targetMessages[i];

                if (message.Role == null) { break; }

                bool isUser = message.Role == "user";
                bool isLastMessage = i == targetMessages.Count - 1;

                string messageContent = message.Content ?? System.Text.Json.JsonSerializer.Serialize(message.Contents, new JsonSerializerOptions { WriteIndented = true });
                var result = UtilityFunctions.ExtractUserAndImageFromMessage(messageContent);
                var messageElement = CreateMessageElement(result.userMessage, isUser, isLastMessage);
                MessagesPanel.Children.Add(messageElement);
                if (result.image != "")
                {
                    var messageElementImage = CreateMessageElement("", false, isLastMessage, result.image);
                    MessagesPanel.Children.Add(messageElementImage);
                }
            }
            MessagesPanel.PreviewMouseWheel += PreviewMouseWheel;
        }
        /// <summary>
        /// メッセージの要素を作成する
        /// </summary>
        private FrameworkElement CreateMessageElement(string messageContent, bool isUser, bool isLastMessage, string visionImage = null)
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

            var copyIcon = new ModernWpf.Controls.SymbolIcon(ModernWpf.Controls.Symbol.Copy)
            {
                Foreground = (Brush)Application.Current.Resources["SystemBaseMediumHighColorBrush"]
            };
            Viewbox copyViewBox = new Viewbox
            {
                Width = 16,
                Child = copyIcon
            };
            Button copyTextButton = new Button
            {
                Width = 30,
                Opacity = 0.5,
                Height = 30,
                Content = copyViewBox,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Padding = new Thickness(0),
                Margin = new Thickness(0, 5, -30, 0),
                Background = Brushes.Transparent,
                Visibility = Visibility.Collapsed
            };
            copyTextButton.Click += (s, e) =>
            {
                CopyTextFromMessageGrid(messageGrid);
                AnimateButtonOpacityToOriginal(copyTextButton, 0.5, TimeSpan.FromMilliseconds(500));
            };
            var translateIcon = new ModernWpf.Controls.SymbolIcon(ModernWpf.Controls.Symbol.Globe)
            {
                Foreground = (Brush)Application.Current.Resources["SystemBaseMediumHighColorBrush"]
            };
            Viewbox viewbox = new Viewbox
            {
                Width = 16,
                Child = translateIcon
            };
            Button translateButton = new Button
            {
                Width = 30,
                Opacity = 0.5,
                Height = 30,
                Content = viewbox,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Padding = new Thickness(0),
                Margin = new Thickness(0, 5, -60, 0),
                Background = Brushes.Transparent,
                Visibility = Visibility.Collapsed
            };
            translateButton.Click += (s, e) =>
            {
                TranslateTextFromMessageGrid(messageGrid);
                AnimateButtonOpacityToOriginal(translateButton, 0.5, TimeSpan.FromMilliseconds(500));
            };

            double opacity = 1;
            if (ThemeManager.Current.ActualApplicationTheme == ModernWpf.ApplicationTheme.Dark)
            {
                opacity = 0.9;
            }
            if (isUser && visionImage == null)
            {
                TextBlock userTextBlock = new TextBlock
                {
                    Padding = new Thickness(10),
                    FontSize = Properties.Settings.Default.FontSize,
                    //Background = accentColorBrush,
                    TextAlignment = TextAlignment.Left,
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = opacity,
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

                Grid.SetColumn(copyTextButton, 1);
                messageGrid.Children.Add(copyTextButton);

                Grid.SetColumn(translateButton, 1);
                messageGrid.Children.Add(translateButton);

                userTextBlock.MouseEnter += ShowButtonOnMouseEnter;
                userTextBlock.MouseLeave += HideButtonOnMouseLeave;
                backgroundRect.MouseEnter += ShowButtonOnMouseEnter;
                backgroundRect.MouseLeave += HideButtonOnMouseLeave;

                // マウスが要素に入ったときにボタンを表示する
                void ShowButtonOnMouseEnter(object s, MouseEventArgs e)
                {
                    copyTextButton.Visibility = Visibility.Visible;
                    translateButton.Visibility = Visibility.Visible;
                }
                void HideButtonOnMouseLeave(object s, MouseEventArgs e)
                {
                    if (copyTextButton.IsMouseOver)
                        return;
                    if (translateButton.IsMouseOver)
                        return;

                    Point mousePosToWindow = Mouse.GetPosition(Application.Current.MainWindow);

                    if (PresentationSource.FromVisual(userTextBlock) != null) // アプリケーションエラー対策
                    {
                        double topBoundary = userTextBlock.PointToScreen(new Point(0, 0)).Y;
                        double bottomBoundary = userTextBlock.PointToScreen(new Point(0, userTextBlock.ActualHeight)).Y;

                        if (mousePosToWindow.Y >= topBoundary && mousePosToWindow.Y <= bottomBoundary)
                        {
                            copyTextButton.Visibility = Visibility.Visible;
                            translateButton.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            copyTextButton.Visibility = Visibility.Collapsed;
                            translateButton.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
            else if (!(isUser) && visionImage == null)
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
                    Opacity = opacity,
                    Document = flowDocument
                };
                richTextBox.Document.FontSize = Properties.Settings.Default.FontSize;
                richTextBox.Document.FontFamily = new FontFamily("Yu Gothic UI");

                ContextMenu contextMenu = CreateContextMenu();
                richTextBox.ContextMenu = contextMenu;

                Grid.SetColumn(richTextBox, 1);
                messageGrid.Children.Add(richTextBox);

                Rectangle backgroundRect = new Rectangle { Fill = Brushes.Transparent };
                Grid.SetColumnSpan(backgroundRect, 3);
                messageGrid.Children.Add(backgroundRect);
                Panel.SetZIndex(backgroundRect, -1);

                Grid.SetColumn(copyTextButton, 1);
                messageGrid.Children.Add(copyTextButton);

                Grid.SetColumn(translateButton, 1);
                messageGrid.Children.Add(translateButton);

                Button regenerateButton = null;
                if (isLastMessage)
                {
                    var icon = new ModernWpf.Controls.SymbolIcon(ModernWpf.Controls.Symbol.Sync)
                    {
                        Foreground = (Brush)Application.Current.Resources["SystemBaseMediumHighColorBrush"]
                    };
                    Viewbox viewBox = new Viewbox
                    {
                        Width = 16,
                        Child = icon
                    };
                    regenerateButton = new Button
                    {
                        Tag = "RegenerateButton",
                        Width = 30,
                        Opacity = 0.5,
                        Height = 30,
                        Content = viewBox,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Padding = new Thickness(0),
                        Margin = new Thickness(0, 5, -90, 0),
                        Background = Brushes.Transparent,
                        Visibility = Visibility.Collapsed
                    };
                    regenerateButton.Click += (s, e) =>
                    {
                        AnimateButtonOpacityToOriginal(regenerateButton, 0.5, TimeSpan.FromMilliseconds(500));
                        RegenerateLatestResponse();
                    };
                    Grid.SetColumn(regenerateButton, 1);
                    messageGrid.Children.Add(regenerateButton);
                }

                richTextBox.MouseEnter += ShowButtonOnMouseEnter;
                richTextBox.MouseLeave += HideButtonOnMouseLeave;
                backgroundRect.MouseEnter += ShowButtonOnMouseEnter;
                backgroundRect.MouseLeave += HideButtonOnMouseLeave;

                // マウスが要素に入ったときにボタンを表示する
                void ShowButtonOnMouseEnter(object s, MouseEventArgs e)
                {
                    copyTextButton.Visibility = Visibility.Visible;
                    translateButton.Visibility = Visibility.Visible;
                    if (regenerateButton != null)
                    {
                        regenerateButton.Visibility = Visibility.Visible;
                    }
                }
                void HideButtonOnMouseLeave(object s, MouseEventArgs e)
                {
                    if (copyTextButton.IsMouseOver)
                        return;
                    if (translateButton.IsMouseOver)
                        return;
                    if (regenerateButton != null && regenerateButton.IsMouseOver)
                        return;

                    Point mousePosToWindow = Mouse.GetPosition(Application.Current.MainWindow);
                    if (PresentationSource.FromVisual(richTextBox) != null) // アプリケーションエラー対策
                    {
                        double topBoundary = richTextBox.PointToScreen(new Point(0, 0)).Y;
                        double bottomBoundary = richTextBox.PointToScreen(new Point(0, richTextBox.ActualHeight)).Y;

                        if (mousePosToWindow.Y >= topBoundary && mousePosToWindow.Y <= bottomBoundary)
                        {
                            copyTextButton.Visibility = Visibility.Visible;
                            translateButton.Visibility = Visibility.Visible;
                            if (regenerateButton != null)
                            {
                                regenerateButton.Visibility = Visibility.Visible;
                            }
                        }
                        else
                        {
                            copyTextButton.Visibility = Visibility.Collapsed;
                            translateButton.Visibility = Visibility.Collapsed;
                            if (regenerateButton != null)
                            {
                                regenerateButton.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                }
            }
            if (visionImage != null)
            {
                string base64Data = visionImage.Substring(visionImage.IndexOf(",") + 1);
                byte[] imageBytes =  Convert.FromBase64String(base64Data); 
                BitmapImage bitmapImage = new BitmapImage();
                using (var ms = new MemoryStream(imageBytes))
                {
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = ms;
                    bitmapImage.EndInit();
                }
                bitmapImage.Freeze();
                Image imageControl = new Image
                {
                    Source = bitmapImage,
                    Stretch = Stretch.Uniform,
                    MaxWidth = 400,
                    MaxHeight = 400,
                };
                messageGrid.Children.Add(imageControl);
                Grid.SetColumn(imageControl, 1);

                // 行全体の背景色を設定する
                Rectangle backgroundRect = new Rectangle { Fill = accentColorBrush };
                Grid.SetColumnSpan(backgroundRect, 3);
                messageGrid.Children.Add(backgroundRect);
                Panel.SetZIndex(backgroundRect, -1);
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
        private PromptTemplateManager templateManager = new PromptTemplateManager();
        private void NewTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PromptTemplateInput();
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                PromptTemplate newTemplate = dialog.Result;

                if (AppSettings.PromptTemplateManager.Templates.Count > 0)
                {
                    int maxSortOrder = AppSettings.PromptTemplateManager.Templates.Max(t => t.SortOrder);
                    newTemplate.SortOrder = maxSortOrder + 1;
                }
                else
                {
                    newTemplate.SortOrder = 1;
                }

                AppSettings.PromptTemplateManager.Templates.Add(newTemplate);
                PromptTemplateListBox.SelectedItem = newTemplate;
                PromptTemplateListBox.ScrollIntoView(newTemplate);
                PromptTemplateListBox.Focus();
            }
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
        private void PromptTemplateEditButton_Click(object sender, RoutedEventArgs e)
        {
            PromptTemplate item = (PromptTemplate)((Button)sender).DataContext;

            var dialog = new PromptTemplateInput(item);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                PromptTemplate newTemplate = dialog.Result;
                item.Title = newTemplate.Title;
                item.Description = newTemplate.Description;
                item.Prompt = newTemplate.Prompt;
                item.LastUpdated = DateTime.Now; // 最終更新日時も更新

                var index = AppSettings.PromptTemplateManager.Templates.IndexOf(item);
                AppSettings.PromptTemplateManager.Templates[index] = item;

                // 変更されたアイテムを再選択
                PromptTemplateListBox.SelectedItem = item;
                PromptTemplateListBox.ScrollIntoView(item);
                PromptTemplateListBox.Items.Refresh();
            }
            else
            {
                PromptTemplateListBox.Items.Refresh();
            }
        }
        private void ConversationFavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            ConversationHistory item = (ConversationHistory)((Button)sender).DataContext;
            item.Favorite = !item.Favorite;
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

            var targetMessages = selectedConversation.Messages;
            for (int i = 0; i < targetMessages.Count; i++)
            {
                var message = targetMessages[i];

                if (message.Role == null) { break; }

                bool isUser = message.Role == "user";
                bool isLastMessage = i == targetMessages.Count - 1;

                // Documents\OpenAIOnWPF\ConversationHistory以外から取得した場合はContentがnullになるので取得元を変更
                string messageContent = message.Content ?? System.Text.Json.JsonSerializer.Serialize(message.Contents, new JsonSerializerOptions { WriteIndented = true });
                var result = UtilityFunctions.ExtractUserAndImageFromMessage(messageContent);

                var messageElement = CreateMessageElement(result.userMessage, isUser, isLastMessage);
                MessagesPanel.Children.Add(messageElement);
                if (result.image != "")
                {
                    var messageElementImage = CreateMessageElement("", false, isLastMessage, result.image);
                    MessagesPanel.Children.Add(messageElementImage);
                }
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

            if (!isFiltering)
            {
                UserTextBox.Focus();
            }
            UserTextBox.CaretIndex = UserTextBox.Text.Length;
        }
        private void PromptTemplateListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTemplate = (PromptTemplate)PromptTemplateListBox.SelectedItem;

            // 削除ボタン活性制御用
            foreach (var item in PromptTemplateListBox.Items.OfType<PromptTemplate>())
            {
                item.IsSelected = false;
            }
            if (selectedTemplate != null)
            {
                selectedTemplate.IsSelected = true;
            }
            foreach (PromptTemplate item in e.RemovedItems)
            {
                item.IsSelected = false;
            }
        }
        private void PromptTemplateSortUpButton_Click(object sender, RoutedEventArgs e)
        {
            PromptTemplate selectedItem = (PromptTemplate)((Button)sender).DataContext;
            int currentIndex = AppSettings.PromptTemplateManager.Templates.IndexOf(selectedItem);
            if (currentIndex > 0)
            {
                PromptTemplate itemAbove = AppSettings.PromptTemplateManager.Templates[currentIndex - 1];

                selectedItem.SortOrder -= 1;
                itemAbove.SortOrder += 1;

                SortTemplatesBySortOrder();
                PromptTemplateListBox.Items.Refresh();
                PromptTemplateListBox.SelectedItem = selectedItem;
                PromptTemplateListBox.ScrollIntoView(selectedItem);
            }
        }
        private void PromptTemplateSortDownButton_Click(object sender, RoutedEventArgs e)
        {
            PromptTemplate selectedItem = (PromptTemplate)((Button)sender).DataContext;
            var templates = AppSettings.PromptTemplateManager.Templates;
            int currentIndex = templates.IndexOf(selectedItem);

            if (currentIndex < templates.Count - 1)
            {
                PromptTemplate itemBelow = templates[currentIndex + 1];

                selectedItem.SortOrder += 1;
                itemBelow.SortOrder -= 1;

                SortTemplatesBySortOrder();
                PromptTemplateListBox.Items.Refresh();
                PromptTemplateListBox.SelectedItem = selectedItem;
                PromptTemplateListBox.ScrollIntoView(selectedItem);
            }
        }
        private void SortTemplatesBySortOrder()
        {
            var sortedTemplates = AppSettings.PromptTemplateManager.Templates
                                   .OrderBy(t => t.SortOrder)
                                   .ToList();

            AppSettings.PromptTemplateManager.Templates.Clear();
            foreach (var template in sortedTemplates)
            {
                AppSettings.PromptTemplateManager.Templates.Add(template);
            }
        }
        private void PromptTemplateInsertButton_Click(object sender, RoutedEventArgs e)
        {
            PromptTemplate selectedItem = (PromptTemplate)((Button)sender).DataContext;
            string prompt = selectedItem.Prompt;
            if (!string.IsNullOrEmpty(UserTextBox.Text))
            {
                // ユーザーに確認するメッセージボックスを表示
                var result = ModernWpf.MessageBox.Show(
                    "The text box already contains text. Do you want to replace it?",
                    "Confirm Replace",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    UserTextBox.Text = prompt;
                }
            }
            else
            {
                UserTextBox.Text = prompt;
            }
        }
        private void PromptTemplateListBox_MouseLeave(object sender, MouseEventArgs e)
        {
            PromptTemplateListBox.SelectedItem = null;
        }
        private void PromptTemplateListBoxItem_MouseEnter(object sender, MouseEventArgs e)
        {
            var item = sender as ListBoxItem;
            if (item != null && !item.IsSelected)
            {
                item.IsSelected = true;
            }
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
                animation?.Stop();
                UserTextBox.Foreground = new SolidColorBrush(initialTextColor);
            }
        }
        private void RegenerateLatestResponse()
        {
            var messages = ConversationListBox.SelectedItems
                .OfType<ConversationHistory>()
                .SelectMany(item => item.Messages)
                .ToList();

            //会話履歴の最新2つを削除
            foreach (ConversationHistory item in ConversationListBox.SelectedItems.OfType<ConversationHistory>())
            {
                if (item.Messages.Count > 1)
                {
                    item.Messages.RemoveAt(item.Messages.Count - 1);
                    item.Messages.RemoveAt(item.Messages.Count - 1);
                }
                else if (item.Messages.Count == 1)
                {
                    item.Messages.RemoveAt(0); // メッセージが1つのみの場合
                }
            }

            if (messages.Count > 1)
            {
                // MessagePanelの下2つを削除
                MessagesPanel.Children.RemoveRange(MessagesPanel.Children.Count - 2, 2);
                _ = ProcessOpenAIAsync(messages[messages.Count - 2].Content);
            }
        }
        private void ApplyFilter(string filterText, bool? isFilteringByFavorite = null)
        {
            var collectionViewSource = FindResource("SortedConversations") as CollectionViewSource;
            if (collectionViewSource != null)
            {
                collectionViewSource.View.Filter = item =>
                {
                    var conversationHistory = item as ConversationHistory;
                    if (conversationHistory != null)
                    {
                        // テキストフィルターの適用
                        bool matchesTextFilter = string.IsNullOrEmpty(filterText) || conversationHistory.Messages.Any(message => message.Content.Contains(filterText, StringComparison.OrdinalIgnoreCase));
                        // お気に入りフィルターの適用(Toggleがfalseの場合はフィルターにFavoriteを使用しない)
                        bool matchesFavoriteFilter = isFilteringByFavorite == null || isFilteringByFavorite.Value == false || conversationHistory.Favorite == isFilteringByFavorite.Value;
                        // 両方の条件を満たすかどうか
                        return matchesTextFilter && matchesFavoriteFilter;
                    }
                    return false;
                };
                collectionViewSource.View.Refresh();
            }
        }
        private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            isFiltering = true; // フィルター時にUserTextBoxにフォーカスが移動しないようにする
            bool? isFilteringByFavorite = FavoriteFilterToggleButton.IsChecked;
            ApplyFilter(FilterTextBox.Text, isFilteringByFavorite);
            isFiltering = false;
        }
        private void ToggleFilterButton_Click(object sender, RoutedEventArgs e)
        {
            FilterTextBox.Visibility = FilterTextBox.Visibility == Visibility.Visible
                   ? Visibility.Collapsed
                   : Visibility.Visible;
            FilterTextBoxClearButton.Visibility = FilterTextBoxClearButton.Visibility == Visibility.Visible
                   ? Visibility.Collapsed
                   : Visibility.Visible;
            FavoriteFilterToggleButton.Visibility = FavoriteFilterToggleButton.Visibility == Visibility.Visible
                   ? Visibility.Collapsed
                   : Visibility.Visible;
            FilterTextBox.Text = string.Empty;
            FavoriteFilterToggleButton.IsChecked = false;
            ApplyFilter("", false);
        }
        private void ClearTextButton_Click(object sender, RoutedEventArgs e)
        {
            FilterTextBox.Text = string.Empty;
        }
        private void FavoriteFilterToggleButton_Click(object sender, RoutedEventArgs e)
        {
            var toggleButton = sender as ToggleButton;
            bool? isFilteringByFavorite = toggleButton.IsChecked;
            ApplyFilter(FilterTextBox.Text, isFilteringByFavorite);
            FavoriteFilterToggleButton.Content = FavoriteFilterToggleButton.IsChecked == true ? "★" : "☆";
        }
        private void AttachFileButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button.ContextMenu != null)
            {
                button.ContextMenu.IsOpen = false;

                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Top;

                button.ContextMenu.IsOpen = true;
            }
        }
        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg;*.webp;*.gif)|*.png;*.jpeg;*.jpg;*.webp;*.gif";
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == true)
            {
                imageFilePath = openFileDialog.FileName;
                ImageFilePathLabel.Content = imageFilePath;
                clipboardImage = null;
            }
        }
        private void PasteFromClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                var image = Clipboard.GetImage();
                using (var memoryStream = new MemoryStream())
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    encoder.Save(memoryStream);
                    clipboardImage = memoryStream.ToArray();
                    imageFilePath = null;
                    ImageFilePathLabel.Content = "clipboard";
                }
            }
            else
            {
                ModernWpf.MessageBox.Show("The clipboard does not contain any images.", "error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            bool imageAvailable = Clipboard.ContainsImage();
            PasteFromClipboardMenuItem.IsEnabled = imageAvailable;
        }
        private void ClearImageFilePathLabelButton_Click(object sender, RoutedEventArgs e)
        {
            imageFilePath = null;
            ImageFilePathLabel.Content = string.Empty;
        }
        private void ImageFilePathLabel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            string argument = $"/select, \"{imageFilePath}\"";
            System.Diagnostics.Process.Start("explorer.exe", argument);
        }
        private void ShowLargeTextInputWindowButton_Click(object sender, RoutedEventArgs e)
        {
            string currentText = UserTextBox.Text;
            var window = new LargeUserTextInput(currentText);
            window.Owner = this;
            window.ShowDialog();
            UserTextBox.Focus();
        }
        private void ToggleVisibilityPromptTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            bool isCollapsed = PromptTemplateListBox.Visibility == Visibility.Collapsed;
            PromptTemplateListBox.Visibility = isCollapsed ? Visibility.Visible : Visibility.Collapsed;
            NewTemplateButton.Visibility = isCollapsed ? Visibility.Visible : Visibility.Collapsed;

            // 折りたたみボタンのアイコンを更新
            ToggleVisibilityPromptTemplateButton.Content = isCollapsed ? "▼" : "▲";

            if (isCollapsed) // 表示
            {
                ChatListGridRow.Height = new GridLength(AppSettings.ChatListGridRowHeightSetting, GridUnitType.Star);
                PromptTemplateGridRow.Height = new GridLength(AppSettings.PromptTemplateGridRowHeightSaveSetting, GridUnitType.Star);
            }
            else // 非表示
            {
                AppSettings.ChatListGridRowHeightSetting = ChatListGridRow.ActualHeight;
                AppSettings.PromptTemplateGridRowHeightSaveSetting = PromptTemplateGridRow.ActualHeight;
                PromptTemplateGridRow.Height = new GridLength(0);
            }

            AppSettings.IsPromptTemplateListVisible = isCollapsed;
        }
    }
}
