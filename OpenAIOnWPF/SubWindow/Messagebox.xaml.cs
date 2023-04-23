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

namespace OpenAIOnWPF
{
    /// <summary>
    /// Messagebox.xaml の相互作用ロジック
    /// </summary>
    public partial class Messagebox
    {
        public Messagebox(string title, string content)
        {
            InitializeComponent();
            OkButton.Focus();
            this.Title = title;
            this.MaxWidth = SystemParameters.PrimaryScreenWidth * 0.8;
            ContentLabel.Text = content;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}
