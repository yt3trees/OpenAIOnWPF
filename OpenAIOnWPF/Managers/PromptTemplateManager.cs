using OpenAIOnWPF.Model;
using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenAIOnWPF
{
    public partial class MainWindow
    {
        private void PromptTemplateListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTemplate = (PromptTemplate)PromptTemplateListBox.SelectedItem;

            // 削除ボタン活性制御用
            foreach (var item in PromptTemplateListBox.Items.OfType<PromptTemplate>())
            {
                item.IsSelected = false;
            }
            if (selectedTemplate != null)
            {
                selectedTemplate.IsSelected = true;
            }
            foreach (PromptTemplate item in e.RemovedItems)
            {
                item.IsSelected = false;
            }
        }
        private void PromptTemplateSortUpButton_Click(object sender, RoutedEventArgs e)
        {
            PromptTemplate selectedItem = (PromptTemplate)((Button)sender).DataContext;
            int currentIndex = AppSettings.PromptTemplateManager.Templates.IndexOf(selectedItem);
            if (currentIndex > 0)
            {
                PromptTemplate itemAbove = AppSettings.PromptTemplateManager.Templates[currentIndex - 1];

                selectedItem.SortOrder -= 1;
                itemAbove.SortOrder += 1;

                SortTemplatesBySortOrder();
                PromptTemplateListBox.Items.Refresh();
                PromptTemplateListBox.SelectedItem = selectedItem;
                PromptTemplateListBox.ScrollIntoView(selectedItem);
            }
        }
        private void PromptTemplateSortDownButton_Click(object sender, RoutedEventArgs e)
        {
            PromptTemplate selectedItem = (PromptTemplate)((Button)sender).DataContext;
            var templates = AppSettings.PromptTemplateManager.Templates;
            int currentIndex = templates.IndexOf(selectedItem);

            if (currentIndex < templates.Count - 1)
            {
                PromptTemplate itemBelow = templates[currentIndex + 1];

                selectedItem.SortOrder += 1;
                itemBelow.SortOrder -= 1;

                SortTemplatesBySortOrder();
                PromptTemplateListBox.Items.Refresh();
                PromptTemplateListBox.SelectedItem = selectedItem;
                PromptTemplateListBox.ScrollIntoView(selectedItem);
            }
        }
        private void SortTemplatesBySortOrder()
        {
            var sortedTemplates = AppSettings.PromptTemplateManager.Templates
                                   .OrderBy(t => t.SortOrder)
                                   .ToList();

            AppSettings.PromptTemplateManager.Templates.Clear();
            foreach (var template in sortedTemplates)
            {
                AppSettings.PromptTemplateManager.Templates.Add(template);
            }
        }
        private void PromptTemplateInsertButton_Click(object sender, RoutedEventArgs e)
        {
            PromptTemplate selectedItem = (PromptTemplate)((Button)sender).DataContext;
            string prompt = selectedItem.Prompt;
            if (!string.IsNullOrEmpty(UserTextBox.Text))
            {
                // ユーザーに確認するメッセージボックスを表示
                var result = ModernWpf.MessageBox.Show(
                    "The text box already contains text. Do you want to replace it?",
                    "Confirm Replace",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    UserTextBox.Text = prompt;
                }
            }
            else
            {
                UserTextBox.Text = prompt;
            }
        }
        private void PromptTemplateListBox_MouseLeave(object sender, MouseEventArgs e)
        {
            PromptTemplateListBox.SelectedItem = null;
        }
        private void PromptTemplateListBoxItem_MouseEnter(object sender, MouseEventArgs e)
        {
            var item = sender as ListBoxItem;
            if (item != null && !item.IsSelected)
            {
                item.IsSelected = true;
            }
        }
        private void PromptTemplateEditButton_Click(object sender, RoutedEventArgs e)
        {
            PromptTemplate item = (PromptTemplate)((Button)sender).DataContext;

            var dialog = new PromptTemplateInput(item);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                PromptTemplate newTemplate = dialog.Result;
                item.Title = newTemplate.Title;
                item.Description = newTemplate.Description;
                item.Prompt = newTemplate.Prompt;
                item.LastUpdated = DateTime.Now; // 最終更新日時も更新

                var index = AppSettings.PromptTemplateManager.Templates.IndexOf(item);
                AppSettings.PromptTemplateManager.Templates[index] = item;

                // 変更されたアイテムを再選択
                PromptTemplateListBox.SelectedItem = item;
                PromptTemplateListBox.ScrollIntoView(item);
                PromptTemplateListBox.Items.Refresh();
            }
            else
            {
                PromptTemplateListBox.Items.Refresh();
            }
        }
        private void NewTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PromptTemplateInput();
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                PromptTemplate newTemplate = dialog.Result;

                if (AppSettings.PromptTemplateManager.Templates.Count > 0)
                {
                    int maxSortOrder = AppSettings.PromptTemplateManager.Templates.Max(t => t.SortOrder);
                    newTemplate.SortOrder = maxSortOrder + 1;
                }
                else
                {
                    newTemplate.SortOrder = 1;
                }

                AppSettings.PromptTemplateManager.Templates.Add(newTemplate);
                PromptTemplateListBox.SelectedItem = newTemplate;
                PromptTemplateListBox.ScrollIntoView(newTemplate);
                PromptTemplateListBox.Focus();
            }
        }
    }
}
