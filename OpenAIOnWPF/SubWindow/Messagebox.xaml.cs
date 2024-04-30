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
        public int resultInt { get; private set; }
        public Messagebox(string title, string content)
        {
            InitializeComponent();
            OkButton.Focus();
            this.Title = title;
            this.MaxWidth = SystemParameters.PrimaryScreenWidth * 0.8;
            ContentLabel.Style = (Style)Application.Current.FindResource("NoBorderTextBoxStyle");
            ContentLabel.Text = content;
        }
        public Messagebox(string title, string content, int integer)
        {
            InitializeComponent();
            OkButton.Focus();
            this.Title = title;
            this.MaxWidth = SystemParameters.PrimaryScreenWidth * 0.8;
            ContentLabel.Style = (Style)Application.Current.FindResource("NoBorderTextBoxStyle");
            ContentLabel.Text = content;
            Numberbox.Visibility = Visibility.Visible;
            Numberbox.Text = integer.ToString();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(Numberbox.Visibility == Visibility.Visible)
            {
                resultInt = int.Parse(Numberbox.Text);
            }
            DialogResult = true;
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
