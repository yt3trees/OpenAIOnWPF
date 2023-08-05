using System.Windows;
using System.Windows.Input;

namespace OpenAIOnWPF
{
    /// <summary>
    /// TitleEditWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TitleEditWindow
    {
        public string NewTitle { get; private set; }
        public TitleEditWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TitleTextBox.Focus();
            TitleTextBox.CaretIndex = TitleTextBox.Text.Length;
        }
        public TitleEditWindow(string currentTitle)
        {
            InitializeComponent();
            TitleTextBox.Text = currentTitle;
        }
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            NewTitle = TitleTextBox.Text;
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
    }
}
