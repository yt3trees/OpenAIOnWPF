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
        public static bool NoticeFlgSetting { get; set; } = Properties.Settings.Default.NoticeFlg;
        public static string? ApiKeySetting { get; set; }
        public static string? ModelSetting { get; set; }
        public static string? ProviderSetting { get; set; }
        public static string? DeploymentIdSetting { get; set; }
        public static string? BaseDomainSetting { get; set; }
        public static string? ApiVersionSetting { get; set; }
        public static int MaxTokensSetting { get; set; }
        public static float TemperatureSetting { get; set; }
    }
}
