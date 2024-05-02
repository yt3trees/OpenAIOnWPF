using MdXaml;
using ModernWpf;
using Newtonsoft.Json.Linq;
using OpenAI.ObjectModels.RequestModels;
using OpenAIOnWPF.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static OpenAIOnWPF.UtilityFunctions;

namespace OpenAIOnWPF
{
    public partial class MainWindow
    {
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
        private void ConversationHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            ShowTable();
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
                    var content = UtilityFunctions.ExtractUserAndImageFromMessage(messageContent);
                    var messageElement = CreateMessageElement(content.userMessage, isUser, isLastMessage);
                    MessagesPanel.Children.Add(messageElement);
                    if (content.image != "")
                    {
                        var messageElementImage = CreateMessageElement("", false, isLastMessage, content.image);
                        MessagesPanel.Children.Add(messageElementImage);
                    }
                }
                MessagesPanel.PreviewMouseWheel += PreviewMouseWheel;
            }
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
                TranslateTextFromMessageGrid(messageGrid, ConversationListBox.SelectedItem);
                AnimateButtonOpacityToOriginal(translateButton, 0.5, TimeSpan.FromMilliseconds(500));
            };

            double opacity = 1;
            if (ThemeManager.Current.ActualApplicationTheme == ModernWpf.ApplicationTheme.Dark)
            {
                opacity = 0.9;
            }
            if (isUser && visionImage == null)
            {
                TextBox userTextBox = new TextBox
                {
                    Padding = new Thickness(10),
                    FontSize = Properties.Settings.Default.FontSize,
                    //Background = accentColorBrush,
                    TextAlignment = TextAlignment.Left,
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = opacity,
                    IsReadOnly = true,
                    Style = (Style)Application.Current.FindResource("NoBorderTextBoxStyle"),
                    Text = messageContent
                };
                userTextBox.MouseDown += UserTextBox_MouseDown;

                ContextMenu contextMenu = CreateContextMenu();
                userTextBox.ContextMenu = contextMenu;

                Grid.SetColumn(userTextBox, 1);
                messageGrid.Children.Add(userTextBox);

                // 行全体の背景色を設定する
                Rectangle backgroundRect = new Rectangle { Fill = accentColorBrush };
                Grid.SetColumnSpan(backgroundRect, 3);
                messageGrid.Children.Add(backgroundRect);
                Panel.SetZIndex(backgroundRect, -1);

                Grid.SetColumn(copyTextButton, 1);
                messageGrid.Children.Add(copyTextButton);

                Grid.SetColumn(translateButton, 1);
                messageGrid.Children.Add(translateButton);

                userTextBox.MouseEnter += ShowButtonOnMouseEnter;
                userTextBox.MouseLeave += HideButtonOnMouseLeave;
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

                    if (PresentationSource.FromVisual(userTextBox) != null) // アプリケーションエラー対策
                    {
                        double topBoundary = userTextBox.PointToScreen(new Point(0, 0)).Y;
                        double bottomBoundary = userTextBox.PointToScreen(new Point(0, userTextBox.ActualHeight)).Y;

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
                MarkdownScrollViewer markDownScrollViewer = new MarkdownScrollViewer();
                markDownScrollViewer.MarkdownStyle = (Style)Application.Current.FindResource("MdXamlStyle");
                markDownScrollViewer.Engine.DisabledContextMenu = true;
                markDownScrollViewer.UseSoftlineBreakAsHardlineBreak = true; // *Options Added in the Forked Version of MdXaml
                markDownScrollViewer.UseDarkThemeSyntaxHighlighting =
                    ThemeManager.Current.ActualApplicationTheme == ModernWpf.ApplicationTheme.Dark; // *Options Added in the Forked Version of MdXaml
                markDownScrollViewer.Markdown = messageContent;
                markDownScrollViewer.Opacity = opacity;
                markDownScrollViewer.SelectionBrush = new SolidColorBrush(ThemeManager.Current.ActualAccentColor);
                markDownScrollViewer.Padding = new Thickness(12, 10, 12, 10);
                markDownScrollViewer.HorizontalContentAlignment = HorizontalAlignment.Left;
                markDownScrollViewer.Document.FontSize = Properties.Settings.Default.FontSize;
                markDownScrollViewer.Document.FontFamily = new FontFamily("Yu Gothic UI");

                markDownScrollViewer.ContextMenuOpening += MarkdwonScroll_ContextMenuOpening;

                void MarkdwonScroll_ContextMenuOpening(object sender, ContextMenuEventArgs e)
                {
                    string paragraphText = "";
                    MarkdownScrollViewer msv = sender as MarkdownScrollViewer;
                    if (msv != null)
                    {
                        // マウス位置の要素を取得
                        var mousePos = Mouse.GetPosition(msv);
                        Visual hitVisual = msv.InputHitTest(mousePos) as Visual;
                        if (hitVisual != null && hitVisual is ICSharpCode.AvalonEdit.Rendering.TextView)
                        {
                            var editor = hitVisual as ICSharpCode.AvalonEdit.Rendering.TextView;
                            paragraphText = editor.Document.Text;
                        }
                        else if (hitVisual != null && hitVisual is Rectangle)
                        {
                            paragraphText = "";
                        }
                        else if (msv != null)
                        {
                            paragraphText = msv.Markdown;
                        }
                        ContextMenu contextMenu = CreateContextMenu(paragraphText);
                        markDownScrollViewer.ContextMenu = contextMenu;
                    }
                }

                Grid.SetColumn(markDownScrollViewer, 1);
                messageGrid.Children.Add(markDownScrollViewer);

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

                markDownScrollViewer.MouseEnter += ShowButtonOnMouseEnter;
                markDownScrollViewer.MouseLeave += HideButtonOnMouseLeave;
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
                    if (PresentationSource.FromVisual(markDownScrollViewer) != null) // アプリケーションエラー対策
                    {
                        double topBoundary = markDownScrollViewer.PointToScreen(new Point(0, 0)).Y;
                        double bottomBoundary = markDownScrollViewer.PointToScreen(new Point(0, markDownScrollViewer.ActualHeight)).Y;

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
                byte[] imageBytes = Convert.FromBase64String(base64Data);
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
        private void RegenerateLatestResponse()
        {
            var messages = ConversationListBox.SelectedItems
                .OfType<ConversationHistory>()
                .SelectMany(item => item.Messages)
                .ToList();

            if (messages.Count > 1)
            {
                string userMessage = messages[messages.Count - 2].Content;
                (string user, string image) result = UtilityFunctions.ExtractUserAndImageFromMessage(userMessage);

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
                // MessagePanelの下2つを削除
                MessagesPanel.Children.RemoveRange(MessagesPanel.Children.Count - 2, 2);

                _ = ProcessOpenAIAsync(result.user);
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
    }
}
