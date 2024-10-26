using OpenAIOnWPF.Model;
using System.Data;

namespace OpenAIOnWPF
{
    public static class AppSettings
    {
        public static DataTable ConfigDataTable { get; set; } = UtilityFunctions.DeserializeDataTable(Properties.Settings.Default.ConfigDataTable);
        public static string SelectConfigSetting { get; set; } = Properties.Settings.Default.SelectConfig;
        public static string InstructionSetting { get; set; } = Properties.Settings.Default.Instruction;
        public static string[,] InstructionListSetting { get; set; } = UtilityFunctions.DeserializeArray(Properties.Settings.Default.InstructionList);
        public static string[,] TokenUsageSetting { get; set; } = UtilityFunctions.DeserializeArray(Properties.Settings.Default.TokenUsage);
        public static bool IsSystemPromptColumnVisible { get; set; } = Properties.Settings.Default.IsSystemPromptColumnVisible;
        public static bool IsConversationColumnVisible { get; set; } = Properties.Settings.Default.IsConversationColumnVisible;
        public static int ConversationHistoryCountSetting { get; set; } = Properties.Settings.Default.ConversationHistoryCount;
        public static bool UseConversationHistoryFlg = Properties.Settings.Default.UseConversationHistory;
        public static ConversationManager ConversationManager { get; set; } 
        public static PromptTemplateManager PromptTemplateManager { get; set; }
        public static double PromptTemplateGridRowHeighSetting = Properties.Settings.Default.PromptTemplateGridRowHeigh;
        public static double ChatListGridRowHeightSetting = Properties.Settings.Default.ChatListGridRowHeight;
        public static double PromptTemplateGridRowHeightSaveSetting = Properties.Settings.Default.PromptTemplateGridRowHeightSave;
        public static string ModelForTitleGenerationSetting = Properties.Settings.Default.ModelForTitleGeneration;
        public static string TitleGenerationPromptSetting = Properties.Settings.Default.TitleGenerationPrompt;
        public static string TitleLanguageSetting = Properties.Settings.Default.TitleLanguage;
        public static bool UseTitleGenerationSetting = Properties.Settings.Default.UseTitleGeneration;
        public static bool IsPromptTemplateListVisible { get; set; } = Properties.Settings.Default.IsPromptTemplateListVisible;
        public static bool NoticeFlgSetting { get; set; } = Properties.Settings.Default.NoticeFlg;
        public static string TranslationAPIProvider { get; set; } = Properties.Settings.Default.TranslationAPIProvider;
        public static bool TranslationAPIUseFlg { get; set; } = Properties.Settings.Default.TranslationAPIUseFlg;
        public static string FromTranslationLanguage { get; set; } = Properties.Settings.Default.FromTranslationLanguage;
        public static string ToTranslationLanguage { get; set; } = Properties.Settings.Default.ToTranslationLanguage;
        public static string TranslationAPIUrlDeepL { get; set; } = Properties.Settings.Default.TranslationAPIUrlDeepL;
        public static string TranslationAPIKeyDeepL { get; set; } = Properties.Settings.Default.TranslationAPIKeyDeepL;
        public static string TranslationAPIUrlGoogle { get; set; } = Properties.Settings.Default.TranslationAPIUrlGoogle;
        public static string TranslationAPIKeyGoogle { get; set; } = Properties.Settings.Default.TranslationAPIKeyGoogle;
        public static string? ApiKeySetting { get; set; }
        public static string? ModelSetting { get; set; }
        public static string? ProviderSetting { get; set; }
        public static string? DeploymentIdSetting { get; set; }
        public static string? BaseDomainSetting { get; set; }
        public static string? ApiVersionSetting { get; set; }
        public static int MaxTokensSetting { get; set; }
        public static float TemperatureSetting { get; set; }
        public static string? BaseModelSetting { get; set; }
    }
}
