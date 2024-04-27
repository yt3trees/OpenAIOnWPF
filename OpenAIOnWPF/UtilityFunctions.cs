using MdXaml;
using ModernWpf;
using ModernWpf.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAIOnWPF.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OpenAIOnWPF
{
    internal class UtilityFunctions
    {
        /// <summary>
        /// 指示内容を生成
        /// </summary>
        /// <returns></returns>
        public static string[] SetupInstructionComboBox()
        {
            string[] instructionList = AppSettings.InstructionListSetting?.Cast<string>().Where((s, i) => i % 2 == 0).ToArray();
            if (instructionList != null)
            {
                Array.Resize(ref instructionList, instructionList.Length + 1);
                instructionList[instructionList.Length - 1] = "";
                return instructionList;
            }
            return null;
        }
        public static void InitializeConfigDataTable()
        {
            if (AppSettings.ConfigDataTable == null)
            {
                DataSet ds = new DataSet();
                AppSettings.ConfigDataTable = new DataTable();
                AppSettings.ConfigDataTable.Columns.Add("ConfigurationName", typeof(string));
                AppSettings.ConfigDataTable.Columns.Add("Provider", typeof(string));
                AppSettings.ConfigDataTable.Columns.Add("Model", typeof(string));
                AppSettings.ConfigDataTable.Columns.Add("APIKey", typeof(string));
                AppSettings.ConfigDataTable.Columns.Add("DeploymentId", typeof(string));
                AppSettings.ConfigDataTable.Columns.Add("BaseDomain", typeof(string));
                AppSettings.ConfigDataTable.Columns.Add("ApiVersion", typeof(string));
                AppSettings.ConfigDataTable.Columns.Add("Temperature", typeof(string));
                AppSettings.ConfigDataTable.Columns.Add("MaxTokens", typeof(string));
                AppSettings.ConfigDataTable.Columns.Add("Vision", typeof(bool));
                ds.Tables.Add(AppSettings.ConfigDataTable);
            }
        }
        public static void InitialColorSet()
        {
            string theme = Properties.Settings.Default.Theme;
            if (theme == "Dark")
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                //SourceChord.FluentWPF.ResourceDictionaryEx.GlobalTheme = SourceChord.FluentWPF.ElementTheme.Dark;
            }
            else if (theme == "Light")
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                //SourceChord.FluentWPF.ResourceDictionaryEx.GlobalTheme = SourceChord.FluentWPF.ElementTheme.Light;
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
        /// <summary>
        /// configデータテーブルに存在していない列があったら追加する
        /// </summary>
        public static void EnsureColumnsForType(DataTable dataTable, Type type)
        {
            foreach (System.Reflection.PropertyInfo propertyInfo in type.GetProperties())
            {
                // 列が存在しない場合に追加
                if (!dataTable.Columns.Contains(propertyInfo.Name))
                {
                    Type columnType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
                    DataColumn column = new DataColumn(propertyInfo.Name, columnType);

                    // デフォルト値を設定
                    if (columnType == typeof(string))
                    {
                        column.DefaultValue = "";
                    }
                    else if (columnType == typeof(bool))
                    {
                        column.DefaultValue = false;
                    }

                    dataTable.Columns.Add(column);
                }
            }
        }
        /// <summary>
        /// メッセージボックスを開く
        /// </summary>
        /// <param name="title">タイトル</param>
        /// <param name="content">内容</param>
        public static void ShowMessagebox(string title, string content)
        {
            var window = new Messagebox(title, content);
            window.Owner = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            window.ShowDialog();
        }
        public static Storyboard CreateOpacityAnimation(DependencyObject target)
        {
            var animation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.5,
                Duration = TimeSpan.FromSeconds(1), // 1秒で1.0から0.5へ変化
                AutoReverse = true,  // 0.5に達した後、再び1に戻る
                RepeatBehavior = RepeatBehavior.Forever // 無限に繰り返す
            };

            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Opacity"));

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);

            return storyboard;
        }
        public static Storyboard CreateTextColorAnimation(TextBox textBox, out Color initialColor)
        {
            // TextBoxの初期のForegroundの色を取得
            initialColor = (textBox.Foreground as SolidColorBrush).Color;

            // 透明度を50%に設定
            Color startColor = initialColor;
            startColor.A = (byte)(255 * 0.5);

            var animation = new ColorAnimation
            {
                From = initialColor,
                To = startColor,
                Duration = TimeSpan.FromSeconds(1),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            Storyboard.SetTarget(animation, textBox);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Foreground.Color"));

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);

            return storyboard;
        }
        public static void AnimateButtonOpacityToOriginal(Button button, double originalOpacity, TimeSpan duration)
        {
            button.Opacity = 1.0;
            var opacityAnimation = new DoubleAnimation
            {
                To = originalOpacity,
                Duration = duration,
                FillBehavior = FillBehavior.Stop
            };
            opacityAnimation.Completed += (s, e) =>
            {
                button.Opacity = originalOpacity;  // 透明度を元の値に設定
            };

            button.BeginAnimation(Button.OpacityProperty, opacityAnimation);
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
        }
        public static string SerializeDataTable(DataTable dataTable)
        {
            //空の場合
            if (dataTable == null)
            {
                return "";
            }
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, dataTable);
                return Convert.ToBase64String(stream.ToArray());
            }
        }
        public static DataTable DeserializeDataTable(string serializedDataTable)
        {
            //空の場合
            if (serializedDataTable == "" || serializedDataTable == null)
            {
                return null;
            }
            using (var stream = new MemoryStream(Convert.FromBase64String(serializedDataTable)))
            {
                var formatter = new BinaryFormatter();
                return (DataTable)formatter.Deserialize(stream);
            }
        }
        public static void CopyTextFromMessageGrid(Grid grid)
        {
            foreach (var child in grid.Children)
            {
                if (child is TextBox textBox)
                {
                    Clipboard.SetText(textBox.Text);
                    break;
                }
                else if (child is MarkdownScrollViewer markdownScrollViewer)
                {
                    Clipboard.SetText(markdownScrollViewer.Markdown);
                    break;
                }
            }
        }
        public static void TranslateTextFromMessageGrid(Grid grid)
        {
            foreach (var child in grid.Children)
            {
                if (child is TextBox textBox)
                {
                    TranslateText(textBox);
                }
                else if (child is MarkdownScrollViewer markdownScrollViewer)
                {
                    TranslateText(markdownScrollViewer);
                }
            }
        }
        public static async void TranslateText(object target)
        {
            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
            Storyboard? animation = null;
            if (target is TextBox textBox)
            {
                try
                {
                    animation = CreateOpacityAnimation(textBox);
                    animation.Begin();

                    string text = textBox.Text;
                    string text2 = await mainWindow.TranslateAPIRequestAsync(text, AppSettings.FromTranslationLanguage);

                    text2 = text2.TrimEnd('\r', '\n');
                    textBox.Text = text2;
                }
                catch (Exception ex)
                {
                    ModernWpf.MessageBox.Show(ex.Message);
                }
                finally
                {
                    animation?.Stop();
                    textBox.Opacity = 1.0;
                }
            }
            else if (target is MarkdownScrollViewer markdownScrollViewer)
            {
                try
                {
                    animation = CreateOpacityAnimation(markdownScrollViewer);
                    animation.Begin();

                    List<(ListItem listItem, Paragraph paragraph, string text)> originalData = new List<(ListItem listItem, Paragraph paragraph, string text)>();
                    foreach (Block block in markdownScrollViewer.Document.Blocks)
                    {
                        // 各ブロックを処理して、originalDataリストにデータを追加
                        ProcessBlocks(new List<Block> { block }, originalData);
                    }

                    // 元のデータの各アイテムを翻訳
                    foreach (var (listItem, paragraph, text) in originalData)
                    {
                        string translatedText = await mainWindow.TranslateAPIRequestAsync(text, AppSettings.FromTranslationLanguage);
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
                    markdownScrollViewer.Opacity = ThemeManager.Current.ActualApplicationTheme == ModernWpf.ApplicationTheme.Dark ? 0.9 : 1;
                }
            }
        }

        public static void ProcessBlocks(IEnumerable<Block> blocks, List<(ListItem listItem, Paragraph paragraph, string text)> originalData)
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
        public static IEnumerable<DependencyObject> GetAllChildren(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                yield return child;
                foreach (var grandChild in GetAllChildren(child))
                {
                    yield return grandChild;
                }
            }
        }
        /// <summary>
        /// message.Contentの中身を解析してメッセージと画像を抽出する
        /// </summary>
        /// <returns></returns>
        public static (string userMessage, string image) ExtractUserAndImageFromMessage(string message)
        {
            JToken token;
            try
            {
                token = JToken.Parse(message);
                if (token.Type != JTokenType.Array)
                {
                    // JSONが配列でない場合はnullとするか、適切なデフォルト値に設定
                    token = null;
                }
            }
            catch (Exception)
            {
                token = null;
            }
            string user = "";
            string image = "";
            if (token != null) // Vision API
            {
                var items = token.ToObject<List<VisionUserContentItem>>();
                foreach (var item in items)
                {
                    if (item.type == "text")
                    {
                        user = item.text;
                    }
                    if ((item.type == "image_url" || item.type == "image") && item.image_url?.url != null)
                    {
                        image = item.image_url.url;
                    }
                }
            }
            else // not Vision API
            {
                user = message;
            }

            return (user, image);
        }
    }
    /// <summary>
    /// ListBoxのFavorite表示用コンバータ
    /// </summary>
    public class FavoriteToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isFavorite = value is bool && (bool)value;
            var symbol = isFavorite ? Symbol.Favorite : Symbol.OutlineStar;

            // SymbolIconを直接返す
            return new SymbolIcon
            {
                Symbol = symbol,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ContentToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ラベルのContentが空でなければVisibility.Visibleを、そうでなければVisibility.Collapsedを返す
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
