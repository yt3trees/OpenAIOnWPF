using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using static OpenAIOnWPF.MainWindow;

namespace OpenAIOnWPF
{
    /// <summary>
    /// Table.xaml の相互作用ロジック
    /// </summary>
    public partial class Table : Window
    {
        public class DataTableItem
        {
            public string? Role { get; set; }
            public string? Content { get; set; }
        }
        public Table(string[,] arg)
        {
            InitializeComponent();
            this.MaxWidth = SystemParameters.PrimaryScreenWidth;
            this.MaxHeight = SystemParameters.PrimaryScreenHeight;
            // レンダリングバグ対応
            SourceInitialized += (s, a) =>
            {
                Dispatcher.Invoke(InvalidateVisual, DispatcherPriority.Input);
            };

            // 会話履歴の保持件数を設定
            Numberbox.Text = conversationHistoryCountSetting.ToString();

            List<DataTableItem> list = new List<DataTableItem>();
            for (int i = 0; i < arg.GetLength(0); i++)
            {
                list.Add(new DataTableItem()
                {
                    Role = arg[i, 0],
                    Content = arg[i, 1]
                });
            }
            DataTable.ItemsSource = list;
        }
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            conversationHistoryCountSetting = int.Parse(Numberbox.Text);
            DialogResult = true;
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
            DialogResult = false;
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
    }
}
