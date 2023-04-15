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
    public partial class TokenUsageWindow : Window
    {
        public class TokenUsageDisplayItem
        { 
            public string? Date { get; set; }
            public string? Provider { get; set; }
            public string? GptVersion { get; set; }
            public string? TokensUsed { get; set; }
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
                tokenUsageDisplayItem.TokensUsed = savedTokenUsage[i, 3];
                TokenUsageDisplayItems.Add(tokenUsageDisplayItem);
            }
            TokenUsageDataGrid.ItemsSource = TokenUsageDisplayItems;

            // 日付を新しい順に並び替える
            TokenUsageDataGrid.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Date", System.ComponentModel.ListSortDirection.Descending));
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
    }
}
