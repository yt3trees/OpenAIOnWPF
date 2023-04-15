using OpenAI.GPT3.ObjectModels.RequestModels;
using System.Collections.Generic;
using System.Data;

namespace OpenAIOnWPF
{
    public static class AppSettings
    {
        public static DataTable ConfigDataTable { get; set; } = MainWindow.DeserializeDataTable(Properties.Settings.Default.ConfigDataTable);
        public static string SelectConfigSetting { get; set; } = Properties.Settings.Default.SelectConfig;
        public static string InstructionSetting { get; set; } = Properties.Settings.Default.Instruction;
        public static string[,] InstructionListSetting { get; set; } = MainWindow.DeserializeArray(Properties.Settings.Default.InstructionList);
        public static string[,] TokenUsageSetting { get; set; } = MainWindow.DeserializeArray(Properties.Settings.Default.TokenUsage);
        public static int ConversationHistoryCountSetting { get; set; } = Properties.Settings.Default.ConversationHistoryCount;
        public static List<ChatMessage> _conversationHistory = new List<ChatMessage>();
        public static List<ChatMessage> ConversationHistory
        {
            get => _conversationHistory;
            set => _conversationHistory = value;
        }
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
