using Microsoft.Extensions.Primitives;
using ModernWpf;
using Newtonsoft.Json;
using OpenAI.GPT3.ObjectModels.RequestModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static OpenAIOnWPF.MainWindow;

namespace OpenAIOnWPF
{
    /// <summary>
    /// Table.xaml の相互作用ロジック
    /// </summary>
    public partial class Table
    {
        public class DataTableItem
        {
            public string? Role { get; set; }
            public string? Content { get; set; }
        }
        public class ViewModel
        {
            public ObservableCollection<string> ComboBoxItems { get; set; }
            public ViewModel()
            {
                ComboBoxItems = new ObservableCollection<string>();
            }
        }
        ViewModel viewModel;
        public Table(string[,] arg)
        {
            InitializeComponent();
            this.MaxWidth = SystemParameters.PrimaryScreenWidth;
            this.MaxHeight = SystemParameters.PrimaryScreenHeight * 0.8;
            // レンダリングバグ対応
            SourceInitialized += (s, a) =>
            {
                Dispatcher.Invoke(InvalidateVisual, DispatcherPriority.Input);
            };

            // 会話履歴の保持件数を設定
            Numberbox.Text = AppSettings.ConversationHistoryCountSetting.ToString();

            viewModel = new ViewModel();
            this.DataContext = viewModel;
            viewModel.ComboBoxItems.Add("user");
            viewModel.ComboBoxItems.Add("assistant");

            ObservableCollection<DataTableItem> list = new ObservableCollection<DataTableItem>();
            for (int i = 0; i < arg.GetLength(0); i++)
            {
                list.Add(new DataTableItem()
                {
                    Role = arg[i, 0],
                    Content = arg[i, 1]
                });
            }
            DataTable.ItemsSource = list;
            if (ThemeManager.Current.ApplicationTheme == ApplicationTheme.Light)
            {
                Brush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#19000000"));
                DataTable.AlternatingRowBackground = brush;
            }
            else
            {
                Brush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#19FFFFFF"));
                DataTable.AlternatingRowBackground = brush;
            }
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.ConversationHistoryCountSetting = int.Parse(Numberbox.Text);

            // 会話履歴を保存
            ObservableCollection<DataTableItem> list = (ObservableCollection<DataTableItem>)DataTable.ItemsSource;
            AppSettings.ConversationHistory.Clear();
            foreach (DataTableItem item in list)
            {
                AppSettings.ConversationHistory.Add(new ChatMessage(item.Role, item.Content));
            }
            string conversationHistoryJson = JsonConvert.SerializeObject(AppSettings.ConversationHistory);
            Properties.Settings.Default.ConversationHistory = conversationHistoryJson;
            Properties.Settings.Default.Save();

            DialogResult = true;
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DialogResult = DialogResult == true;
        }
        private void dataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            ((DataGridTextColumn)e.Column).EditingElementStyle = (Style)this.Resources["editingTextBoxStyle"];
        }
        private void editingTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (Key.Return == e.Key && 0 < (ModifierKeys.Shift & e.KeyboardDevice.Modifiers))
            {
                TextBox tb = (TextBox)sender;
                int caret = tb.CaretIndex;
                tb.Text = tb.Text.Insert(caret, "\r\n");
                tb.CaretIndex = caret + 1;
                e.Handled = true;
            }
        }
        private void DataTable_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataTable.Columns.Count > 0)
            {
                // 2列目のスタイル設定
                DataTable.Columns[1].SetValue(DataGridTextColumn.ElementStyleProperty, new Style(typeof(TextBlock))
                {
                    Setters = {
                        new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap),
                        new Setter(TextBlock.PaddingProperty, new Thickness(5,5,5,5))
                    }
                });
                DataTable.Columns[1].Width = new DataGridLength(1.0, DataGridLengthUnitType.Star);

                // ComboBox用の新しいDataGridTemplateColumnを作成
                DataGridTemplateColumn comboBoxColumn = new DataGridTemplateColumn();
                comboBoxColumn.Header = DataTable.Columns[0].Header;

                // ComboBoxのFrameworkElementFactoryを作成
                FrameworkElementFactory comboBoxFactory = new FrameworkElementFactory(typeof(ComboBox));

                // ComboBox ItemsSourceプロパティのバインディングを設定
                Binding itemsSourceBinding = new Binding
                {
                    Path = new PropertyPath("ComboBoxItems"),
                    Mode = BindingMode.OneWay,
                    Source = viewModel
                };
                comboBoxFactory.SetBinding(ComboBox.ItemsSourceProperty, itemsSourceBinding);

                // ComboBox SelectedItemプロパティのバインディングを設定
                Binding selectedItemBinding = new Binding
                {
                    Path = new PropertyPath("Role"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                comboBoxFactory.SetValue(ComboBox.SelectedItemProperty, selectedItemBinding);

                // ComboBoxの幅を100で固定
                comboBoxFactory.SetValue(ComboBox.WidthProperty, 100.0);

                // DataTemplateを作成し、そのビジュアルツリーにComboBoxを設定
                DataTemplate cellTemplate = new DataTemplate();
                cellTemplate.VisualTree = comboBoxFactory;

                // DataTemplateをDataGridTemplateColumnに割り当て
                comboBoxColumn.CellTemplate = cellTemplate;

                // 指定したインデックスの既存の列を削除
                DataTable.Columns.RemoveAt(0);

                // 指定したインデックスに新しいComboBox列を挿入
                DataTable.Columns.Insert(0, comboBoxColumn);
            }
        }
    }
}
