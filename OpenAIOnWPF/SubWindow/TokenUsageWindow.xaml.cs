using ModernWpf;
using System;
using System.Collections.Generic;
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
using static OpenAIOnWPF.UtilityFunctions;

namespace OpenAIOnWPF
{
    /// <summary>
    /// TokenUsageWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TokenUsageWindow
    {
        public class TokenUsageDisplayItem
        { 
            public string? Date { get; set; }
            public string? Provider { get; set; }
            public string? GptVersion { get; set; }
            public string? TotalTokenUsage { get; set; }
            public string? InputTokenUsage { get; set; }
            public string? OutputTokenUsage { get; set; }
        }
        public TokenUsageWindow()
        {
            InitializeComponent();

            string[,] savedTokenUsage = DeserializeArray(Properties.Settings.Default.TokenUsage);

            List<TokenUsageDisplayItem> TokenUsageDisplayItems = new List<TokenUsageDisplayItem>();

            for (int i = 0; i < savedTokenUsage.GetLength(0); i++)
            {
                TokenUsageDisplayItem tokenUsageDisplayItem = new TokenUsageDisplayItem();
                tokenUsageDisplayItem.Date = savedTokenUsage[i, 0];
                tokenUsageDisplayItem.Provider = savedTokenUsage[i, 1];
                tokenUsageDisplayItem.GptVersion = savedTokenUsage[i, 2];
                tokenUsageDisplayItem.TotalTokenUsage = int.Parse(savedTokenUsage[i, 3]).ToString("N0");

                int inputTokens = 0;
                if (savedTokenUsage.GetLength(1) > 4 && int.TryParse(savedTokenUsage[i, 4], out inputTokens))
                {
                    tokenUsageDisplayItem.InputTokenUsage = inputTokens.ToString("N0");
                }
                else
                {
                    tokenUsageDisplayItem.InputTokenUsage = "0";
                }

                int outputTokens = 0;
                if (savedTokenUsage.GetLength(1) > 5 && int.TryParse(savedTokenUsage[i, 5], out outputTokens))
                {
                    tokenUsageDisplayItem.OutputTokenUsage = outputTokens.ToString("N0");
                }
                else
                {
                    tokenUsageDisplayItem.OutputTokenUsage = "0";
                }

                TokenUsageDisplayItems.Add(tokenUsageDisplayItem);
            }
            TokenUsageDataGrid.ItemsSource = TokenUsageDisplayItems;

            // 日付を新しい順に並び替える
            TokenUsageDataGrid.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Date", System.ComponentModel.ListSortDirection.Descending));

            AlertSettingButton.Content = $"Set Alert Threshold: {Properties.Settings.Default.dailyTokenThreshold}";

            var accentColor = ThemeManager.Current.AccentColor;
            if (accentColor == null)
            {
                accentColor = SystemParameters.WindowGlassColor;
            }
            var accentColorBrush = new SolidColorBrush((Color)accentColor);
            OkButton.Background = accentColorBrush;
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void AlertSettingButton_Click(object sender, RoutedEventArgs e)
        {
            int threshold = Properties.Settings.Default.dailyTokenThreshold;
            var window = new Messagebox("Set an alert threshold", "You will be alerted when daily token usage exceeds this threshold.", threshold);
            window.Owner = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (window.ShowDialog() == true)
            {
                Properties.Settings.Default.dailyTokenThreshold = window.resultInt;
                Properties.Settings.Default.Save();
                AlertSettingButton.Content = $"Set Alert Threshold: {Properties.Settings.Default.dailyTokenThreshold}";
            }
        }
        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var dataContext = button.DataContext;
            string input = (dataContext as TokenUsageDisplayItem).InputTokenUsage.Replace(",", "");
            string output = (dataContext as TokenUsageDisplayItem).OutputTokenUsage.Replace(",", "");;

            var calculationWindow = new TokenCalculator(input, output);
            calculationWindow.Owner = this;
            calculationWindow.ShowDialog();
        }
    }
}
