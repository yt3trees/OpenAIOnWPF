using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenAIOnWPF
{
    /// <summary>
    /// LargeUserTextInput.xaml の相互作用ロジック
    /// </summary>
    public partial class LargeUserTextInput
    {
        public string NewTitle { get; private set; }
        public LargeUserTextInput()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UserLargeTextBox.Focus();
            UserLargeTextBox.CaretIndex = UserLargeTextBox.Text.Length;
        }
        public LargeUserTextInput(string text)
        {
            InitializeComponent();
            UserLargeTextBox.Text = text;
        }
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow.UserTextBox.Text = this.UserLargeTextBox.Text;
            this.DialogResult = true;
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
        private void UserLargeTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
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
    }
}
