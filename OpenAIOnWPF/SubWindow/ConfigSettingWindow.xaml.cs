using ModernWpf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static OpenAIOnWPF.MainWindow;

namespace OpenAIOnWPF
{
    /// <summary>
    /// ConfigSettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ConfigSettingWindow
    {
        //public string[,] inputResult => items;
        internal class ModelList
        {
            public string? ConfigurationName { get;  set; }
            public string? Provider { get;  set; }
            public string? Model { get;  set; }
            public string? APIKey { get;  set; }
            public string? DeploymentId { get;  set; }
            public string? BaseDomain { get;  set; }
            public string? ApiVersion { get;  set; }
            public string? Temperature { get;  set; }
            public string? MaxTokens { get;  set; }
            public bool? Vision { get;  set; }
        }
        public ConfigSettingWindow()
        {
            InitializeComponent();

            //var color = ThemeManager.Current.ActualAccentColor;
            //SaveButton.Background = new SolidColorBrush(color);

            ConfigListBox.ContextMenu = new ContextMenu();
            MenuItem UpSwap = new MenuItem();
            UpSwap.Header = "⬆";
            UpSwap.Click += UpSwap_Click;
            UpSwap.HorizontalAlignment = HorizontalAlignment.Center;
            MenuItem DownSwap = new MenuItem();
            DownSwap.Header = "⬇";
            DownSwap.Click += DownSwap_Click;
            DownSwap.HorizontalAlignment = HorizontalAlignment.Center;
            ConfigListBox.ContextMenu.Items.Add(UpSwap);
            ConfigListBox.ContextMenu.Items.Add(DownSwap);

            var accentColor = ThemeManager.Current.AccentColor;
            if (accentColor == null)
            {
                accentColor = SystemParameters.WindowGlassColor;
            }
            var accentColorBrush = new SolidColorBrush((Color)accentColor);
            SaveButton.Background = accentColorBrush;

            ModelComboBox.Items.Add("gpt-3.5-turbo");
            ModelComboBox.Items.Add("gpt-3.5-turbo-16k");
            ModelComboBox.Items.Add("gpt-3.5-turbo-instruct");
            ModelComboBox.Items.Add("gpt-3.5-turbo-1106");
            ModelComboBox.Items.Add("gpt-3.5-turbo-0125");
            ModelComboBox.Items.Add("gpt-4");
            ModelComboBox.Items.Add("gpt-4-32k");
            ModelComboBox.Items.Add("gpt-4-0613");
            ModelComboBox.Items.Add("gpt-4-1106-preview");
            ModelComboBox.Items.Add("gpt-4-0125-preview");
            ModelComboBox.Items.Add("gpt-4-vision-preview");
            ModelComboBox.Items.Add("gpt-4-turbo");
            ModelComboBox.Items.Add("gpt-4-turbo-2024-04-09");
            ModelComboBox.Items.Add("gpt-4o");
            ModelComboBox.Items.Add("gpt-4o-mini");

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
            //AppSettings.ConfigDataTable.Rows.Add("Default", "OpenAI", "gpt-3.5-turbo", "APIKey", "DeploymentId", "BaseDomain", "ApiVersion", "0.2", "2048");

            foreach (DataRow row in AppSettings.ConfigDataTable.Rows)
            {
                ConfigListBox.Items.Add(row["ConfigurationName"]);
                //ConfigListBox.SelectedIndex = 0;
            }
            var index = AppSettings.ConfigDataTable.Rows.IndexOf(AppSettings.ConfigDataTable.AsEnumerable().Where(a => a.Field<string>(0) == AppSettings.SelectConfigSetting).FirstOrDefault());
            ConfigListBox.SelectedIndex = index;
        }
        private void DuplicateControl()
        {
            //重複している名前に*をつける
            for (int i = 0; i < AppSettings.ConfigDataTable.Rows.Count; i++)
            {
                string currentName = AppSettings.ConfigDataTable.Rows[i]["ConfigurationName"].ToString();

                for (int j = 0; j < AppSettings.ConfigDataTable.Rows.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    if (AppSettings.ConfigDataTable.Rows[i]["ConfigurationName"].ToString() == AppSettings.ConfigDataTable.Rows[j]["ConfigurationName"].ToString())
                    {
                        currentName += "*";
                        AppSettings.ConfigDataTable.Rows[j]["ConfigurationName"] = currentName;
                    }
                }
            }
        }
        private void Save()
        {
            if (ConfigListBox.SelectedIndex == -1)
            {
                return;
            }
            if (ConfigurationNameTextBox.Text == "")
            {
                ModernWpf.MessageBox.Show("The Configuration name has not been entered.", "Error", MessageBoxButton.OK);
                return;
            }

            // 入力内容をAppSettings.ConfigDataTableに保存
            int index = ConfigListBox.SelectedIndex;
            AppSettings.ConfigDataTable.Rows[index]["ConfigurationName"] = ConfigurationNameTextBox.Text;
            AppSettings.ConfigDataTable.Rows[index]["Provider"] = ProviderComboBox.Text;
            AppSettings.ConfigDataTable.Rows[index]["APIKey"] = APIKeyPasswordbox.Password;
            AppSettings.ConfigDataTable.Rows[index]["Model"] = ModelComboBox.Text;
            AppSettings.ConfigDataTable.Rows[index]["DeploymentId"] = DeploymentIdTextbox.Text;
            AppSettings.ConfigDataTable.Rows[index]["BaseDomain"] = BaseDomainTextbox.Text;
            AppSettings.ConfigDataTable.Rows[index]["ApiVersion"] = ApiVersionTextbox.Text;
            AppSettings.ConfigDataTable.Rows[index]["Temperature"] = TemperatureNumberbox.Text;
            AppSettings.ConfigDataTable.Rows[index]["MaxTokens"] = MaxTokensNumberbox.Text;
            AppSettings.ConfigDataTable.Rows[index]["Vision"] = VisionToggleSwitch.IsOn;

            DuplicateControl();
            ConfigListBox.SelectedIndex = index;
            ConfigListBox.Items[index] = AppSettings.ConfigDataTable.Rows[index]["ConfigurationName"];
        }
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ConfigListBox.SelectedItem == null) return;
            if (ConfigListBox.SelectedIndex == -1)
            {
                return;
            }
            ConfigurationNameTextBox.Text = AppSettings.ConfigDataTable.Rows[ConfigListBox.SelectedIndex]["ConfigurationName"].ToString();
            ProviderComboBox.Text = AppSettings.ConfigDataTable.Rows[ConfigListBox.SelectedIndex]["Provider"].ToString();
            APIKeyPasswordbox.Password = AppSettings.ConfigDataTable.Rows[ConfigListBox.SelectedIndex]["APIKey"].ToString();
            ModelComboBox.Text = AppSettings.ConfigDataTable.Rows[ConfigListBox.SelectedIndex]["Model"].ToString();
            DeploymentIdTextbox.Text = AppSettings.ConfigDataTable.Rows[ConfigListBox.SelectedIndex]["DeploymentId"].ToString();
            BaseDomainTextbox.Text = AppSettings.ConfigDataTable.Rows[ConfigListBox.SelectedIndex]["BaseDomain"].ToString();
            ApiVersionTextbox.Text = AppSettings.ConfigDataTable.Rows[ConfigListBox.SelectedIndex]["ApiVersion"].ToString();
            TemperatureNumberbox.Text = AppSettings.ConfigDataTable.Rows[ConfigListBox.SelectedIndex]["Temperature"].ToString();
            MaxTokensNumberbox.Text = AppSettings.ConfigDataTable.Rows[ConfigListBox.SelectedIndex]["MaxTokens"].ToString();
            VisionToggleSwitch.IsOn = Convert.ToBoolean(AppSettings.ConfigDataTable.Rows[ConfigListBox.SelectedIndex]["Vision"]);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            //confgDataTableに1つ追加
            DataRow row = AppSettings.ConfigDataTable.NewRow();
            row["ConfigurationName"] = "New Item";
            row["Provider"] = "OpenAI";
            row["Temperature"] = "1";
            row["MaxTokens"] = "2048";
            row["Vision"] = false;
            AppSettings.ConfigDataTable.Rows.Add(row);
            ConfigListBox.Items.Add(row["ConfigurationName"].ToString());
            ConfigListBox.SelectedIndex = ConfigListBox.Items.Count - 1;
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            //選択しているアイテムを削除
            int index = ConfigListBox.SelectedIndex;
            if (index == -1) return;
            //ListBoxから削除
            ConfigListBox.Items.RemoveAt(index);
            //AppSettings.ConfigDataTableから削除
            AppSettings.ConfigDataTable.Rows[index].Delete();
            //選択を一つ上に
            ConfigListBox.SelectedIndex = index - 1;
        }
        private void SwapItems(int index, bool isUp)
        {
            if (isUp)
            {
                //上に移動
                if (index == 0) return;
                //AppSettings.ConfigDataTableの入れ替え
                DataRow row = AppSettings.ConfigDataTable.NewRow();
                row.ItemArray = AppSettings.ConfigDataTable.Rows[index - 1].ItemArray.Clone() as object[];
                AppSettings.ConfigDataTable.Rows[index - 1].ItemArray = AppSettings.ConfigDataTable.Rows[index].ItemArray.Clone() as object[];
                AppSettings.ConfigDataTable.Rows[index].ItemArray = row.ItemArray.Clone() as object[];
                //ListBoxの入れ替え
                string name = ConfigListBox.Items[index - 1].ToString();
                ConfigListBox.Items[index - 1] = ConfigListBox.Items[index];
                ConfigListBox.Items[index] = name;
                ConfigListBox.SelectedIndex = index - 1;
            }
            else
            {
                //下に移動
                if (index == ConfigListBox.Items.Count - 1) return;
                //AppSettings.ConfigDataTableの入れ替え
                DataRow row = AppSettings.ConfigDataTable.NewRow();
                row.ItemArray = AppSettings.ConfigDataTable.Rows[index + 1].ItemArray.Clone() as object[];
                AppSettings.ConfigDataTable.Rows[index + 1].ItemArray = AppSettings.ConfigDataTable.Rows[index].ItemArray.Clone() as object[];
                AppSettings.ConfigDataTable.Rows[index].ItemArray = row.ItemArray.Clone() as object[];
                //ListBoxの入れ替え
                string name = ConfigListBox.Items[index + 1].ToString();
                ConfigListBox.Items[index + 1] = ConfigListBox.Items[index];
                ConfigListBox.Items[index] = name;
                ConfigListBox.SelectedIndex = index + 1;
            }
        }
        private void UpSwap()
        {
            int index = ConfigListBox.SelectedIndex;
            SwapItems(index, true);
        }
        private void DownSwap()
        {
            int index =  ConfigListBox.SelectedIndex;
            SwapItems(index, false);
        }
        void UpSwap_Click(object sender, RoutedEventArgs e)
        {
            UpSwap();
        }
        void DownSwap_Click(object sender, RoutedEventArgs e)
        {
            DownSwap();
        }
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //ConfigListBoxをjsonファイルに出力
                List<ModelList> items = new List<ModelList>();
                foreach (DataRow row in AppSettings.ConfigDataTable.Rows)
                {
                    ModelList item = new ModelList();
                    item.ConfigurationName = row["ConfigurationName"].ToString();
                    item.Provider = row["Provider"].ToString();
                    item.Model = row["Model"].ToString();
                    item.APIKey = row["APIKey"].ToString();
                    item.DeploymentId = row["DeploymentId"].ToString();
                    item.BaseDomain = row["BaseDomain"].ToString();
                    item.ApiVersion = row["ApiVersion"].ToString();
                    item.Temperature = row["Temperature"].ToString();
                    item.MaxTokens = row["MaxTokens"].ToString();
                    item.Vision = Convert.ToBoolean(row["Vision"].ToString());
                    items.Add(item);
                }
                
                string json = JsonConvert.SerializeObject(items);
                json = JToken.Parse(json).ToString(Formatting.Indented);

                var dialog = new System.Windows.Forms.SaveFileDialog();
                dialog.Title = "Please select an export file.";
                dialog.FileName = DateTime.Now.ToString("yyyyMMdd") + "_config.json";
                dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                dialog.DefaultExt = "json";
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    File.WriteAllText(dialog.FileName, json);
                    ModernWpf.MessageBox.Show("Exported successfully.");
                }
            }
            catch(Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.Message);
            }
        }
        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var okFlg = ModernWpf.MessageBox.Show("Overwrite with the contents of the selected json file. Are you sure?", "Question", MessageBoxButton.YesNo);
                if (okFlg == MessageBoxResult.Yes)
                {
                    // jsonファイルを読み込み
                    var dialog = new System.Windows.Forms.OpenFileDialog();
                    dialog.Title = "Please select a json file.";
                    dialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
                    dialog.FilterIndex = 1;
                    dialog.RestoreDirectory = true;
                    System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        string path = dialog.FileName;
                        string json = File.ReadAllText(path);
                        //itemsにjsonの値を格納する
                        List<ModelList> items = JsonConvert.DeserializeObject<List<ModelList>>(json);

                        // ListBoxにアイテムを再セット
                        AppSettings.ConfigDataTable.Rows.Clear();
                        foreach (ModelList item in items)
                        {
                            DataRow row = AppSettings.ConfigDataTable.NewRow();
                            row["ConfigurationName"] = item.ConfigurationName;
                            row["Provider"] = item.Provider;
                            row["Model"] = item.Model;
                            row["APIKey"] = item.APIKey;
                            row["DeploymentId"] = item.DeploymentId;
                            row["BaseDomain"] = item.BaseDomain;
                            row["ApiVersion"] = item.ApiVersion;
                            row["Temperature"] = item.Temperature;
                            row["MaxTokens"] = item.MaxTokens;
                            row["Vision"] = item.Vision;
                            AppSettings.ConfigDataTable.Rows.Add(row);
                        }
                        ConfigListBox.Items.Clear();
                        foreach (DataRow row in AppSettings.ConfigDataTable.Rows)
                        {
                            ConfigListBox.Items.Add(row["ConfigurationName"]);
                        }
                        
                        ModernWpf.MessageBox.Show("Imported successfully.");
                    }
                }

                DuplicateControl();
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.Message);
            }
        }
        private void ConfigListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.K)
            {
                UpSwap();
            }
            if (e.Key == Key.J)
            {
                DownSwap();
            }
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DuplicateControl();
            DialogResult = true;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
            }
            if (e.Key == Key.Delete)
            {
                 RemoveButton_Click(sender ,e);
            }
            //control+d
            if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Control)
            {
                AddButton_Click(sender, e);
            }
        }
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Save();
            }
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Save();
            }
        }
        private void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProviderComboBox.SelectedItem == null) return;
            if (ProviderComboBox.SelectedItem.ToString() == "System.Windows.Controls.ComboBoxItem: OpenAI")
            {
                ModelComboBox.IsEnabled = true;
                DeploymentIdTextbox.IsEnabled = false;
                ApiVersionTextbox.IsEnabled = false;
                BaseDomainTextbox.IsEnabled = false;
            }
            else
            {
                ModelComboBox.IsEnabled = false;
                DeploymentIdTextbox.IsEnabled = true;
                ApiVersionTextbox.IsEnabled = true;
                BaseDomainTextbox.IsEnabled = true;
            }
            ConfigurationNameTextBox.Focus();
        }
    }
}
