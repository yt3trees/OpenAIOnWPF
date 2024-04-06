using OpenAIOnWPF.Model;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenAIOnWPF
{
    /// <summary>
    /// PromptTemplateInput.xaml の相互作用ロジック
    /// </summary>
    public partial class PromptTemplateInput
    {
        public PromptTemplate Result { get; set; }
        public PromptTemplate TargetTemplate { get; set; }
        public string Title { get; set; }
        public PromptTemplateInput()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Title != null)
            {
                PromptTextBox.Focus();
                PromptTextBox.CaretIndex = PromptTextBox.Text.Length;
            }
            else
            {
                TitleTextBox.Focus();
            }
        }
        //public PromptTemplateInput(string? title = null, string? description = null, string? prompt = null)
        public PromptTemplateInput(PromptTemplate promptTemplate)
        {
            InitializeComponent();
            TargetTemplate = promptTemplate;

            Title = promptTemplate.Title;
            if (promptTemplate != null)
            {
                TitleTextBox.Text = promptTemplate.Title;
                DescriptionTextBox.Text = promptTemplate.Description;
                PromptTextBox.Text = promptTemplate.Prompt;
            }
        }
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
            {
                return;
            }

            Result = new PromptTemplate()
            {
                Title = TitleTextBox.Text,
                Description = DescriptionTextBox.Text,
                Prompt = PromptTextBox.Text
            };
            this.DialogResult = true;
        }
        private bool ValidateInput()
        {
            // 必須入力フィールドが空かどうかを確認
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text) ||
                // string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ||
                string.IsNullOrWhiteSpace(PromptTextBox.Text))
            {
                // 入力が不足している場合はユーザーに通知
                ModernWpf.MessageBox.Show("All fields are required.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
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
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                OkButton_Click(sender, e);
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DialogResult = DialogResult == true;
        }
        private void PromptTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                var textBox = sender as TextBox;

                if (e.Delta > 0 && textBox.FontSize < 40)
                {
                    textBox.FontSize += 2;
                }
                else if (e.Delta < 0 && textBox.FontSize > 10)
                {
                    textBox.FontSize -= 2;
                }
                e.Handled = true;
            }
        }
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var result = ModernWpf.MessageBox.Show(
                    "Are you sure you want to delete this item?",
                    "Delete Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                if (TargetTemplate != null)
                {
                    int sortOrderToDelete = TargetTemplate.SortOrder;
                    AppSettings.PromptTemplateManager.Templates.Remove(TargetTemplate);
                    foreach (var item in AppSettings.PromptTemplateManager.Templates.Where(t => t.SortOrder > sortOrderToDelete))
                    {
                        // 各アイテムのSortOrderをデクリメント
                        item.SortOrder--;
                    }
                    this.DialogResult = false;
                }
            }
        }
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
            {
                return;
            }

            Result = new PromptTemplate()
            {
                Title = TitleTextBox.Text,
                Description = DescriptionTextBox.Text,
                Prompt = PromptTextBox.Text
            };

            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (!string.IsNullOrEmpty(mainWindow.UserTextBox.Text))
            {
                // ユーザーに確認するメッセージボックスを表示
                var result = ModernWpf.MessageBox.Show(
                    "The text box already contains text. Do you want to replace it?",
                    "Confirm Replace",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    mainWindow.UserTextBox.Text = PromptTextBox.Text;
                }
                else
                {
                    return;
                }
            }
            else
            {
                mainWindow.UserTextBox.Text = PromptTextBox.Text;
            }

            this.DialogResult = true;
        }
    }
}
