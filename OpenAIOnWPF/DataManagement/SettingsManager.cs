using OpenAIOnWPF.Model;
using System;
using System.Text;
using System.Windows;
using static OpenAIOnWPF.DataManagement.DataManager;
using static OpenAIOnWPF.UtilityFunctions;

namespace OpenAIOnWPF.DataManagement
{
    public class SettingsManager
    {
        /// <summary>
        /// 設定内容を保存
        /// </summary>
        public static void SaveSettings()
        {
            SaveGeneralSettings();
            SaveUISettings();
            SaveTranslationSettings();
            TitleGenerationSettings();
            SaveConversationSettings();
            SaveTemplateSettings();
            Properties.Settings.Default.Save();
        }

        private static void SaveGeneralSettings()
        {
            Properties.Settings.Default.ConfigDataTable = SerializeDataTable(AppSettings.ConfigDataTable);
            Properties.Settings.Default.SelectConfig = AppSettings.SelectConfigSetting;

            Properties.Settings.Default.UseConversationHistory = AppSettings.UseConversationHistoryFlg;
            Properties.Settings.Default.ConversationHistoryCount = AppSettings.ConversationHistoryCountSetting;

            Properties.Settings.Default.NoticeFlg = AppSettings.NoticeFlgSetting;
            Properties.Settings.Default.Instruction = AppSettings.InstructionSetting;
            Properties.Settings.Default.InstructionList = SerializeArray(AppSettings.InstructionListSetting);

        }
        private static void SaveUISettings()
        {
            Properties.Settings.Default.IsSystemPromptColumnVisible = AppSettings.IsSystemPromptColumnVisible;
            Properties.Settings.Default.IsConversationColumnVisible = AppSettings.IsConversationColumnVisible;
            Properties.Settings.Default.IsPromptTemplateListVisible = AppSettings.IsPromptTemplateListVisible;
            Properties.Settings.Default.PromptTemplateGridRowHeigh = AppSettings.PromptTemplateGridRowHeighSetting;
            Properties.Settings.Default.ChatListGridRowHeight = AppSettings.ChatListGridRowHeightSetting;
            Properties.Settings.Default.PromptTemplateGridRowHeightSave = AppSettings.PromptTemplateGridRowHeightSaveSetting;
        }
        private static void SaveTranslationSettings()
        {
            Properties.Settings.Default.TranslationAPIProvider = AppSettings.TranslationAPIProvider;
            Properties.Settings.Default.TranslationAPIUseFlg = AppSettings.TranslationAPIUseFlg;
            Properties.Settings.Default.FromTranslationLanguage = AppSettings.FromTranslationLanguage;
            Properties.Settings.Default.ToTranslationLanguage = AppSettings.ToTranslationLanguage;
            Properties.Settings.Default.TranslationAPIUrlDeepL = AppSettings.TranslationAPIUrlDeepL;
            Properties.Settings.Default.TranslationAPIKeyDeepL = AppSettings.TranslationAPIKeyDeepL;
            Properties.Settings.Default.TranslationAPIUrlGoogle = AppSettings.TranslationAPIUrlGoogle;
            Properties.Settings.Default.TranslationAPIKeyGoogle = AppSettings.TranslationAPIKeyGoogle;
        }
        private static void TitleGenerationSettings()
        {
            Properties.Settings.Default.ModelForTitleGeneration = AppSettings.ModelForTitleGenerationSetting;
            Properties.Settings.Default.TitleGenerationPrompt = AppSettings.TitleGenerationPromptSetting;
            Properties.Settings.Default.TitleLanguage = AppSettings.TitleLanguageSetting;
            Properties.Settings.Default.UseTitleGeneration = AppSettings.UseTitleGenerationSetting;
        }
        private static void SaveConversationSettings()
        {
            SaveConversationsAsJson(AppSettings.ConversationManager);
        }
        private static void SaveTemplateSettings()
        {
            SavePromptTemplateAsJson(AppSettings.PromptTemplateManager);
        }
        public static void InitializeSettings()
        {
            // 前バージョンの設定を引き継ぐ
            if (Properties.Settings.Default.UpgradeRequired)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
                Properties.Settings.Default.Save();
            }
            try
            {
                AppSettings.ConversationManager = DataManagement.DataManager.LoadConversationsFromJson();
                AppSettings.PromptTemplateManager = DataManagement.DataManager.LoadPromptTemplateFromJson();
            }
            catch (Exception ex)
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var message = new StringBuilder()
                    .AppendLine("Failed to load conversation history.")
                    .AppendLine(ex.Message)
                    .AppendLine()
                    .AppendLine("Do you want to reset the conversation history and settings?")
                    .AppendLine("If you choose No, the application will exit at this point.")
                    .AppendLine("Restart the application after the problem is resolved.")
                    .AppendLine()
                    .AppendLine($"{documentsPath}\\OpenAIOnWPF\\ConversationHistory")
                    .ToString();
                var result = ModernWpf.MessageBox.Show(
                    message,
                    "Error",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Error
                );
                if (result == MessageBoxResult.Yes)
                {
                    AppSettings.ConversationManager = new ConversationManager();
                }
                else
                {
                    Environment.Exit(1);
                }
            }
        }
    }
}
