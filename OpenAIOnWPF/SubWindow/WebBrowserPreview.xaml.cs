using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenAIOnWPF
{
    /// <summary>
    /// WebBrowserPreview.xaml の相互作用ロジック
    /// </summary>
    public partial class WebBrowserPreview
    {
        private string html;
        public WebBrowserPreview()
        {
            InitializeComponent();
        }
        public WebBrowserPreview(string htmlContent)
        {
            InitializeComponent();
            html = htmlContent;
            Loaded += async (sender, e) =>
            {
                await webView.EnsureCoreWebView2Async(null);
                webView.CoreWebView2.NavigateToString(htmlContent);
            };
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                ToggleEditMode_Click(sender, e);
            }
        }
        private void WebView_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            webView.CoreWebView2.Settings.IsZoomControlEnabled = true;
        }
        private void SaveHtmlButton_Click(object sender, RoutedEventArgs e)
        {
            SaveHtmlToFile();
        }
        private void SaveHtmlToFile()
        {
            string htmlContent = html;
            string dateTimeSuffix = DateTime.Now.ToString("yyyyMMddHHmmss");

            var saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog.Filter = "HTML files (*.html)|*.html|All files (*.*)|*.*";
            saveFileDialog.DefaultExt = "html";
            saveFileDialog.FileName = $"MermaidDiagram_{dateTimeSuffix}.html";

            var result = saveFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog.FileName, htmlContent);
                ModernWpf.MessageBox.Show("Exported successfully.");
            }
        }
        private bool isInEditMode = false;
        private void ToggleEditMode_Click(object sender, RoutedEventArgs e)
        {
            if (isInEditMode)
            {
                // 編集モードからプレビューモードへ切り替え
                html = editTextBox.Text;
                webView.Visibility = Visibility.Visible;
                editTextBox.Visibility = Visibility.Collapsed;
                webView.CoreWebView2.NavigateToString(editTextBox.Text); // 編集内容をWebViewに反映
                isInEditMode = false;
            }
            else
            {
                // プレビューモードから編集モードへ切り替え
                webView.Visibility = Visibility.Collapsed;
                editTextBox.Visibility = Visibility.Visible;
                editTextBox.Text = html;  // 最初のHTMLコンテンツをテキストボックスにセット
                isInEditMode = true;
            }
        }
        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            webView.ZoomFactor *= 1.1;
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            webView.ZoomFactor /= 1.1;
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
