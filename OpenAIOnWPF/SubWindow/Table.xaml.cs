using Microsoft.Extensions.Primitives;
using ModernWpf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI.ObjectModels.RequestModels;
using OpenAIOnWPF.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
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
        public ConversationHistory UpdatedConversationHistory { get; private set; }
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
        public Table(ConversationHistory conversationHistory)
        {
            InitializeComponent();
            //// レンダリングバグ対応
            //SourceInitialized += (s, a) =>
            //{
            //    Dispatcher.Invoke(InvalidateVisual, DispatcherPriority.Input);
            //};

            // 会話履歴の保持件数を設定
            Numberbox.Text = AppSettings.ConversationHistoryCountSetting.ToString();

            viewModel = new ViewModel();
            this.DataContext = viewModel;
            viewModel.ComboBoxItems.Add("user");
            viewModel.ComboBoxItems.Add("assistant");

            ObservableCollection<DataTableItem> list = new ObservableCollection<DataTableItem>();
            foreach (var message in conversationHistory.Messages)
            {
                list.Add(new DataTableItem() { Role = message.Role, Content = message.Content });
            }
            DataTable.ItemsSource = list;
            if (ThemeManager.Current.ApplicationTheme == ApplicationTheme.Light)
            {
                System.Windows.Media.Brush brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#19000000"));
                DataTable.AlternatingRowBackground = brush;
            }
            else
            {
                System.Windows.Media.Brush brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#19FFFFFF"));
                DataTable.AlternatingRowBackground = brush;
            }
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.ConversationHistoryCountSetting = int.Parse(Numberbox.Text);

            // 会話履歴を保存
            ObservableCollection<DataTableItem> list = (ObservableCollection<DataTableItem>)DataTable.ItemsSource;
            UpdatedConversationHistory = new ConversationHistory();
            foreach (DataTableItem item in list)
            {
                UpdatedConversationHistory.Messages.Add(new ChatMessage(item.Role, item.Content));
            }

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

            // 右クリックメニュー
            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = new MenuItem();
            menuItem.Header = "Add new row after selected row";
            menuItem.Icon = new ModernWpf.Controls.SymbolIcon(ModernWpf.Controls.Symbol.Add);
            menuItem.Click += AddNewRowBeforeSelected_Click;
            contextMenu.Items.Add(menuItem);

            MenuItem deleteMenuItem = new MenuItem();
            deleteMenuItem.Header = "Delete selected row";
            deleteMenuItem.Icon = new ModernWpf.Controls.SymbolIcon(ModernWpf.Controls.Symbol.Delete);
            deleteMenuItem.Click += DeleteSelectedRow_Click;
            contextMenu.Items.Add(deleteMenuItem);

            DataTable.ContextMenu = contextMenu;
        }
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataTable.Items.Count <= 1)
            {
                ModernWpf.MessageBox.Show("No conversation history.");
                return;
            }
            var dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.Title = "Please select an export file.";
            string fileName = DateTime.Now.ToString("yyyyMMdd") + "_";
            if (((DataTableItem)DataTable.Items[0]).Content.Length < 20)
            {
                fileName += ((DataTableItem)DataTable.Items[0]).Content.Substring(0, ((DataTableItem)DataTable.Items[0]).Content.Length).Replace("/", "").Replace(":", "");
            }
            else
            {
                fileName += ((DataTableItem)DataTable.Items[0]).Content.Substring(0, 20).Replace("/", "").Replace(":", "") + "~";
            }
            dialog.FileName = fileName;
            dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            dialog.DefaultExt = "json";

            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string json = JsonConvert.SerializeObject(DataTable.ItemsSource);
                json = JToken.Parse(json).ToString(Formatting.Indented);
                string path = dialog.FileName;
                File.WriteAllText(path, json);
                ModernWpf.MessageBox.Show("Exported successfully.");
            }
        }
        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Title = "Please select an import file.";
            dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string path = dialog.FileName;
                string json = File.ReadAllText(path);
                ObservableCollection<DataTableItem> list = JsonConvert.DeserializeObject<ObservableCollection<DataTableItem>>(json);
                DataTable.ItemsSource = list;
                DataTable.Columns.RemoveAt(1);
                DataTable_Loaded(null, null);
                ModernWpf.MessageBox.Show("Imported successfully.");
            }
        }
        private void AddNewRowBeforeSelected_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = DataTable.SelectedIndex;

            if (selectedIndex >= 0)
            {
                DataTableItem item = new DataTableItem();
                item.Role = "User";
                item.Content = "";
                (DataTable.ItemsSource as ObservableCollection<DataTableItem>).Insert(selectedIndex + 1, item);
            }
            else
            {
                DataTableItem item = new DataTableItem();
                item.Role = "User";
                item.Content = "";
                (DataTable.ItemsSource as ObservableCollection<DataTableItem>).Insert(0, item);
            }
        }
        private void DeleteSelectedRow_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = DataTable.SelectedIndex;

            if (selectedIndex >= 0)
            {
                (DataTable.ItemsSource as ObservableCollection<DataTableItem>).RemoveAt(selectedIndex);
            }
        }
        private void AcrylicWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Height = this.Owner.Height;
            this.Top = this.Owner.Top;
        }
        /// <summary>
        /// 親のScrollViewerでスクロールする(Gridをセル単位ではなくピクセル単位でスクロールできるようにする)
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
    }
}
