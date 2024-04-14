using OpenAIOnWPF.Model;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace OpenAIOnWPF.DataManagement
{
    internal class DataManager
    {
        public static ConversationManager LoadConversationsFromJson()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string dataDirectory = Path.Combine(documentsPath, "OpenAIOnWPF", "ConversationHistory");

            var manager = new ConversationManager();
            manager.Histories = new ObservableCollection<ConversationHistory>();

            Directory.CreateDirectory(dataDirectory);

            string[] files = Directory.GetFiles(dataDirectory, "Conversation_*.json");

            foreach (var file in files)
            {
                string jsonString = File.ReadAllText(file);
                ConversationHistory conversation = System.Text.Json.JsonSerializer.Deserialize<ConversationHistory>(jsonString);

                if (conversation != null)
                {
                    manager.Histories.Add(conversation);
                }
            }
            return manager;
        }
        public static void SaveConversationsAsJson(ConversationManager manager)
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string dataDirectory = Path.Combine(documentsPath, "OpenAIOnWPF", "ConversationHistory");

            Directory.CreateDirectory(dataDirectory);

            foreach (var file in Directory.EnumerateFiles(dataDirectory, "*.json"))
            {
                File.Delete(file);
            }

            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 非ASCII文字をエスケープしない
            };

            foreach (var conversation in manager.Histories)
            {
                string formattedLastUpdated = conversation.LastUpdated.ToString("yyyyMMddHHmmss");
                string filePath = Path.Combine(dataDirectory, $"Conversation_{formattedLastUpdated}_{conversation.ID}.json");
                string jsonString = System.Text.Json.JsonSerializer.Serialize(conversation, options);

                File.WriteAllText(filePath, jsonString);
            }
        }
        public static void SavePromptTemplateAsJson(PromptTemplateManager manager)
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string dataDirectory = Path.Combine(documentsPath, "OpenAIOnWPF", "PromptTemplate");

            Directory.CreateDirectory(dataDirectory);

            foreach (var file in Directory.EnumerateFiles(dataDirectory, "*.json"))
            {
                File.Delete(file);
            }

            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 非ASCII文字をエスケープしない
            };

            foreach (var template in manager.Templates)
            {
                string formattedLastUpdated = template.LastUpdated.ToString("yyyyMMddHHmmss");
                string filePath = Path.Combine(dataDirectory, $"PromptTemplate_{formattedLastUpdated}_{template.ID}.json");
                string jsonString = System.Text.Json.JsonSerializer.Serialize(template, options);

                File.WriteAllText(filePath, jsonString);
            }
        }
        public static PromptTemplateManager LoadPromptTemplateFromJson()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string dataDirectory = Path.Combine(documentsPath, "OpenAIOnWPF", "PromptTemplate");

            var manager = new PromptTemplateManager();
            manager.Templates = new ObservableCollection<PromptTemplate>();

            Directory.CreateDirectory(dataDirectory);

            string[] files = Directory.GetFiles(dataDirectory, "PromptTemplate_*.json");

            foreach (var file in files)
            {
                string jsonString = File.ReadAllText(file);
                PromptTemplate templates = System.Text.Json.JsonSerializer.Deserialize<PromptTemplate>(jsonString);

                if (templates != null)
                {
                    manager.Templates.Add(templates);
                }
            }
            return manager;
        }
    }
}
