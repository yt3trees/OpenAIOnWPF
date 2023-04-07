using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenAIOnWPF
{
    /// <summary>
    /// InstructionSettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class InstructionSettingWindow : Window
    {
        public string[,] inputResult { get { return ReturnItems(); } }
        string[,] items { get; set; }
        public InstructionSettingWindow(string[,] param)
        {
            InitializeComponent();
            items = param;

            // itemsがnullなら[0,0]で初期化
            if (items == null)
            {
                items = new string[1, 2];
                items[0, 0] = "";
                items[0, 1] = "";
            }

            //itemsの1列目をInstructionListBoxに格納
            for (int i = 0; i < items.GetLength(0); i++)
            {
                InstructionListBox.Items.Add(items[i, 0]);
            }
            InstructionListBox.SelectedIndex = 0;
        }
        private void Save()
        {
            int index = InstructionListBox.SelectedIndex;
            items[index, 0] = InstructionTextBox.Text;
            items[index, 1] = ContentsTextBox.Text;

            InstructionListBox.Items.Clear();
            for (int i = 0; i < items.GetLength(0); i++)
            {
                InstructionListBox.Items.Add(items[i, 0]);
            }
            InstructionListBox.SelectedIndex = index;
        }
        private string[,] ReturnItems()
        {
            return items;
        }
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InstructionListBox.SelectedItem == null) return;
            if (InstructionListBox.SelectedIndex == -1)
            {
                return;
            }
            InstructionTextBox.Text = items[InstructionListBox.SelectedIndex, 0];
            ContentsTextBox.Text = items[InstructionListBox.SelectedIndex, 1];
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
            }
        }

        private void ContentsTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Save();
            }
        }

        private void InstructionTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Save();
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // itemsの行数を1増やす
            string[,] newItems = new string[items.GetLength(0) + 1, 2];
            for (int i = 0; i < items.GetLength(0); i++)
            {
                newItems[i, 0] = items[i, 0];
                newItems[i, 1] = items[i, 1];
            }
            newItems[items.GetLength(0), 0] = "new item";
            newItems[items.GetLength(0), 1] = "";
            items = newItems;

            // 再セット
            InstructionListBox.Items.Clear();
            for (int i = 0; i < items.GetLength(0); i++)
            {
                InstructionListBox.Items.Add(items[i, 0]);
            }
            InstructionListBox.SelectedIndex = items.GetLength(0) - 1;
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            //選択しているアイテムを削除
            int index = InstructionListBox.SelectedIndex;
            if (index == -1)
            {
                return;
            }
            string[,] newItems = new string[items.GetLength(0) - 1, 2];
            for (int i = 0; i < index; i++)
            {
                newItems[i, 0] = items[i, 0];
                newItems[i, 1] = items[i, 1];
            }
            for (int i = index + 1; i < items.GetLength(0); i++)
            {
                newItems[i - 1, 0] = items[i, 0];
                newItems[i - 1, 1] = items[i, 1];
            }
            items = newItems;

            // 再セット
            InstructionListBox.Items.Clear();
            for (int i = 0; i < items.GetLength(0); i++)
            {
                InstructionListBox.Items.Add(items[i, 0]);
            }
            InstructionListBox.SelectedIndex = items.GetLength(0) - 1;
        }

        private void InstructionListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.K)
            {
                //InstructionListBoxのアイテムを上と入れ替える
                int index = InstructionListBox.SelectedIndex;
                if (index == -1)
                {
                    return;
                }
                if (index == 0)
                {
                    return;
                }
                string[,] newItems = new string[items.GetLength(0), 2];
                for (int i = 0; i < index - 1; i++)
                {
                    newItems[i, 0] = items[i, 0];
                    newItems[i, 1] = items[i, 1];
                }
                newItems[index - 1, 0] = items[index, 0];
                newItems[index - 1, 1] = items[index, 1];
                newItems[index, 0] = items[index - 1, 0];
                newItems[index, 1] = items[index - 1, 1];
                for (int i = index + 1; i < items.GetLength(0); i++)
                {
                    newItems[i, 0] = items[i, 0];
                    newItems[i, 1] = items[i, 1];
                }
                items = newItems;
                // 再セット
                InstructionListBox.Items.Clear();
                for (int i = 0; i < items.GetLength(0); i++)
                {
                    InstructionListBox.Items.Add(items[i, 0]);
                }
                InstructionListBox.SelectedIndex = items.GetLength(0) - 1;
                InstructionListBox.SelectedIndex = index - 1;
            }
            if (e.Key == Key.J)
            {
                //InstructionListBoxのアイテムを下と入れ替える
                int index = InstructionListBox.SelectedIndex;
                if (index == -1)
                {
                    return;
                }
                if (index == items.GetLength(0) - 1)
                {
                    return;
                }
                string[,] newItems = new string[items.GetLength(0), 2];
                for (int i = 0; i < index; i++)
                {
                    newItems[i, 0] = items[i, 0];
                    newItems[i, 1] = items[i, 1];
                }
                newItems[index, 0] = items[index + 1, 0];
                newItems[index, 1] = items[index + 1, 1];
                newItems[index + 1, 0] = items[index, 0];
                newItems[index + 1, 1] = items[index, 1];
                for (int i = index + 2; i < items.GetLength(0); i++)
                {
                    newItems[i, 0] = items[i, 0];
                    newItems[i, 1] = items[i, 1];
                }
                items = newItems;
                // 再セット
                InstructionListBox.Items.Clear();
                for (int i = 0; i < items.GetLength(0); i++)
                {
                    InstructionListBox.Items.Add(items[i, 0]);
                }
                InstructionListBox.SelectedIndex = items.GetLength(0) - 1;
                InstructionListBox.SelectedIndex = index + 1;
            }
        }
    }
}
