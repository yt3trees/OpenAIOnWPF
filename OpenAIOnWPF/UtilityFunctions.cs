using ModernWpf;
using Newtonsoft.Json;
using OpenAIOnWPF.Model;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OpenAIOnWPF
{
    internal class UtilityFunctions
    {
        /// <summary>
        /// 設定内容を保存
        /// </summary>
        public static void SaveSettings()
        {
            Properties.Settings.Default.ConversationHistoryCount = AppSettings.ConversationHistoryCountSetting;
            Properties.Settings.Default.NoticeFlg = AppSettings.NoticeFlgSetting;
            Properties.Settings.Default.Instruction = AppSettings.InstructionSetting;
            Properties.Settings.Default.InstructionList = SerializeArray(AppSettings.InstructionListSetting);
            Properties.Settings.Default.ConfigDataTable = SerializeDataTable(AppSettings.ConfigDataTable);
            Properties.Settings.Default.SelectConfig = AppSettings.SelectConfigSetting;
            Properties.Settings.Default.UseConversationHistory = AppSettings.UseConversationHistoryFlg;
            Properties.Settings.Default.IsSystemPromptColumnVisible = AppSettings.IsSystemPromptColumnVisible;
            Properties.Settings.Default.IsConversationColumnVisible = AppSettings.IsConversationColumnVisible;
            Properties.Settings.Default.TranslationAPIProvider = AppSettings.TranslationAPIProvider;
            Properties.Settings.Default.TranslationAPIUseFlg = AppSettings.TranslationAPIUseFlg;
            Properties.Settings.Default.FromTranslationLanguage = AppSettings.FromTranslationLanguage;
            Properties.Settings.Default.ToTranslationLanguage = AppSettings.ToTranslationLanguage;
            Properties.Settings.Default.TranslationAPIUrlDeepL = AppSettings.TranslationAPIUrlDeepL;
            Properties.Settings.Default.TranslationAPIKeyDeepL = AppSettings.TranslationAPIKeyDeepL;
            Properties.Settings.Default.TranslationAPIUrlGoogle = AppSettings.TranslationAPIUrlGoogle;
            Properties.Settings.Default.TranslationAPIKeyGoogle = AppSettings.TranslationAPIKeyGoogle;
            Properties.Settings.Default.Save();
            SaveConversationsAsJson(AppSettings.ConversationManager);
        }
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
        /// <summary>
        /// 設定画面を開く
        /// </summary>
        /// <param name="targetSetting">設定対象</param>
        /// <param name="content">現在の設定</param>
        /// <param name="type">設定値の種別</param>
        /// <returns></returns>
        public static string ShowSetting(string targetSetting, string content, string type)
        {
            var window = new Setting(targetSetting, content, type);
            window.Owner = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            bool result = (bool)window.ShowDialog();
            return result ? window.inputResult : "";
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
                return null;            }
            using (var stream = new MemoryStream(Convert.FromBase64String(serializedDataTable)))
            {
                var formatter = new BinaryFormatter();
                return (DataTable)formatter.Deserialize(stream);
            }
        }
        public static void SaveConversationsAsJson(ConversationManager manager)
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string dataDirectory = Path.Combine(documentsPath, "OpenAIOnWPF", "ConversationHistory");

            Directory.CreateDirectory(dataDirectory);

            foreach (var file in Directory.EnumerateFiles(dataDirectory, "*.json"))
            {
                File.Delete(file);
            }

            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 非ASCII文字をエスケープしない
            };

            foreach (var conversation in manager.Histories)
            {
                string formattedLastUpdated = conversation.LastUpdated.ToString("yyyyMMddHHmmss");
                string filePath = Path.Combine(dataDirectory, $"Conversation_{formattedLastUpdated}_{conversation.ID}.json");
                string jsonString = System.Text.Json.JsonSerializer.Serialize(conversation, options);

                File.WriteAllText(filePath, jsonString);
            }
        }
        public static ConversationManager LoadConversationsFromJson()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string dataDirectory = Path.Combine(documentsPath, "OpenAIOnWPF", "ConversationHistory");

            var manager = new ConversationManager();
            manager.Histories = new ObservableCollection<ConversationHistory>();

            Directory.CreateDirectory(dataDirectory);

            string[] files = Directory.GetFiles(dataDirectory, "Conversation_*.json");

            foreach (var file in files)
            {
                string jsonString = File.ReadAllText(file);
                ConversationHistory conversation = System.Text.Json.JsonSerializer.Deserialize<ConversationHistory>(jsonString);

                if (conversation != null)
                {
                    manager.Histories.Add(conversation);
                }
            }
            return manager;
        }
    }
}
