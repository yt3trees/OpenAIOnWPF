using ModernWpf;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static OpenAIOnWPF.MainWindow;

namespace OpenAIOnWPF
{
    /// <summary>
    /// ColorSettings.xaml の相互作用ロジック
    /// </summary>
    public partial class ColorSettings
    {
        public static IReadOnlyList<string> KnownColorNames { get; } =
            typeof(Colors)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Select(info => (info.Name))
            .ToArray();
        public static ApplicationTheme? BefTheme { get; set; }
        public static Color? BefAccent { get; set; }
        public static bool OkFlg { get; set; }

        public ColorSettings()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if(ThemeManager.Current.AccentColor !=  null)
            {
                var color = ThemeManager.Current.AccentColor;
                //OkButton.Background = new SolidColorBrush((Color)color);
            }
            else
            {
                var color = ThemeManager.Current.ActualAccentColor;
                //OkButton.Background = new SolidColorBrush(color);
            }

            OkFlg = false;
            AccentColorList.ItemsSource = KnownColorNames;
            BefTheme = ThemeManager.Current.ApplicationTheme;
            BefAccent = ThemeManager.Current.AccentColor;

            if (ThemeManager.Current.ApplicationTheme == ApplicationTheme.Dark)
            {
                ThemeDark.IsChecked = true;
            }
            else if (ThemeManager.Current.ApplicationTheme == ApplicationTheme.Light)
            {
                ThemeLight.IsChecked = true;
            }
            else
            {
                ThemeSystem.IsChecked = true;
            }

            if (ThemeManager.Current.AccentColor == null)
            {
                AccentColorSystem.IsChecked = true;
            }
            else
            {
                AccentColorSet.IsChecked = true;
                AccentColorList.SelectedItem = ThemeManager.Current.AccentColor.ToString();
            }
        }
        /// <summary>
        /// 閉じるボタン押下時
        /// </summary>
        protected virtual void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (OkFlg == false)
            {
                ThemeManager.Current.ApplicationTheme = BefTheme;
                ThemeManager.Current.AccentColor = BefAccent;
            }
        }
        private void ThemeRadio_Click(object sender, RoutedEventArgs e)
        {
            var ctrl = sender as Control;
            if (ctrl == ThemeLight)
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                SourceChord.FluentWPF.ResourceDictionaryEx.GlobalTheme = SourceChord.FluentWPF.ElementTheme.Light;
            }
            else if (ctrl == ThemeDark)
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                SourceChord.FluentWPF.ResourceDictionaryEx.GlobalTheme = SourceChord.FluentWPF.ElementTheme.Dark;
            }
            else
            {
                ThemeManager.Current.ApplicationTheme = null;
                SourceChord.FluentWPF.ResourceDictionaryEx.GlobalTheme = null;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            OkFlg = true;
            if (ThemeManager.Current.ApplicationTheme == ApplicationTheme.Dark)
            {
                Properties.Settings.Default.Theme = "Dark";
            }
            else if (ThemeManager.Current.ApplicationTheme == ApplicationTheme.Light)
            {
                Properties.Settings.Default.Theme = "Light";
            }
            else
            {
                 Properties.Settings.Default.Theme = "Default";
            }

            if (ThemeManager.Current.AccentColor == null)
            {
                Properties.Settings.Default.AccentColor = "Default";
            }
            else
            {
                Properties.Settings.Default.AccentColor = ThemeManager.Current.AccentColor.ToString();
            }

            DialogResult = true;
        }
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
            }
        }

        private void AccentColorSystem_Checked(object sender, RoutedEventArgs e)
        {
            ThemeManager.Current.AccentColor = null;
            AccentColorList.IsEnabled = false;
        }

        private void AccentColorSet_Checked(object sender, RoutedEventArgs e)
        {
            AccentColorList.IsEnabled = true;
        }

        private void AccentColorList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var color = (Color)ColorConverter.ConvertFromString(AccentColorList.SelectedValue.ToString());
            ThemeManager.Current.AccentColor = color;
            //OkButton.Background = new SolidColorBrush(color);
        }
    }
}
