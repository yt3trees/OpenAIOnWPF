using Microsoft.Toolkit.Uwp.Notifications;
using ModernWpf;
using Newtonsoft.Json;
using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.Tokenizer.GPT3;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;

namespace OpenAIOnWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// DataBinding用クラス
        /// </summary>
        public class DataBind
        {
            public string? PlaceHolder { get; set; }
        }
        string selectInstructionContent = "";
        string userMessage = "";
        Stopwatch stopWatch = new Stopwatch();

        public MainWindow()
        {
            InitializeComponent();
            InitializeSettings();
        }
        private void InitializeSettings()
        {
            InitialColorSet();
            UserTextBox.Focus();
            NoticeCheckbox.IsChecked = AppSettings.NoticeFlgSetting;
            this.DataContext = new DataBind { PlaceHolder = "Send a message..." };

            SetupInstructionComboBox();

            var appSettings = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal);
            Debug.Print("Path to save the configuration file:" + appSettings.FilePath);

            if (AppSettings.ConfigDataTable == null)
            {
                InitializeConfigDataTable();
            }
            ConfigurationComboBox.ItemsSource = AppSettings.ConfigDataTable.AsEnumerable().Select(x => x.Field<string>("ConfigurationName")).ToList();
            ConfigurationComboBox.Text = AppSettings.SelectConfigSetting;
        }
        private void SetupInstructionComboBox()
        {
            string[] instructionList = AppSettings.InstructionListSetting?.Cast<string>().Where((s, i) => i % 2 == 0).ToArray();
            if (instructionList != null)
            {
                Array.Resize(ref instructionList, instructionList.Length + 1);
                instructionList[instructionList.Length - 1] = "";
                InstructionComboBox.ItemsSource = instructionList;
                InstructionComboBox.Text = String.IsNullOrEmpty(AppSettings.InstructionSetting) ? "" : AppSettings.InstructionSetting;
            }
        }
        private void InitializeConfigDataTable()
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
        /// <summary>
        /// メッセージボックスを開く
        /// </summary>
        /// <param name="title">タイトル</param>
        /// <param name="content">内容</param>
        private void ShowMessagebox(string title, string content)
        {
            var window = new Messagebox(title,content);
            window.Owner = this;
            window.ShowDialog();
        }
        /// <summary>
        /// 設定画面を開く
        /// </summary>
        /// <param name="targetSetting">設定対象</param>
        /// <param name="content">現在の設定</param>
        /// <param name="type">設定値の種別</param>
        /// <returns></returns>
        private string ShowSetting(string targetSetting, string content, string type)
        {
            var window = new Setting(targetSetting, content, type);
            window.Owner = this;
            bool result = (bool)window.ShowDialog();
            return result ? window.inputResult : "";
        }
        private void ShowTable()
        {
            int count = AppSettings.ConversationHistory.Count;
            string[,] table = new string[count, 2];
            foreach (var item in AppSettings.ConversationHistory)
            {
                table[AppSettings.ConversationHistory.IndexOf(item), 0] = item.Role;
                table[AppSettings.ConversationHistory.IndexOf(item), 1] = item.Content;
            }
            var window = new Table(table);
            window.Owner = this;
            window.ShowDialog();
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
            //return JsonConvert.DeserializeObject<string[,]>(serializedArray);
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
        /// <summary>
        /// 指示内容を設定する
        /// </summary>
        private void  InstructionSettingWindowOpen()
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
        public static void InitialColorSet()
        {
            string theme = Properties.Settings.Default.Theme;
            if (theme == "Dark")
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            }
            else if (theme == "Light")
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
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
    }
}
