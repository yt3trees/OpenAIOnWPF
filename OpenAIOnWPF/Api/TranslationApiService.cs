using Google.Cloud.Translation.V2;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace OpenAIOnWPF
{
    public partial class MainWindow
    {
        public async Task<string> TranslateAPIRequestAsync(string inputText, string targetLang)
        {
            if (AppSettings.TranslationAPIProvider == "DeepL")
            {
                if (string.IsNullOrWhiteSpace(AppSettings.TranslationAPIKeyDeepL))
                {
                    throw new Exception("Translate API Key is not set.");
                }
                if (string.IsNullOrWhiteSpace(AppSettings.TranslationAPIUrlDeepL))
                {
                    throw new Exception("Translate API URL is not set.");
                }

                using (var client = new HttpClient())
                {
                    try
                    {
                        var content = new FormUrlEncodedContent(new[]
                        {
                        new KeyValuePair<string, string>("text", inputText),
                        new KeyValuePair<string, string>("target_lang", targetLang),
                    });

                        client.DefaultRequestHeaders.Add("Authorization", $"DeepL-Auth-Key {AppSettings.TranslationAPIKeyDeepL}");

                        var response = await client.PostAsync(AppSettings.TranslationAPIUrlDeepL, content);
                        var responseBody = await response.Content.ReadAsStringAsync();

                        var json = JObject.Parse(responseBody);
                        return json["translations"][0]["text"].ToString();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"API request failed: {ex.Message}");
                    }
                }
            }
            else if (AppSettings.TranslationAPIProvider == "Google")
            {
                if (string.IsNullOrWhiteSpace(AppSettings.TranslationAPIKeyGoogle))
                {
                    throw new Exception("Translate API Key is not set.");
                }
                if (string.IsNullOrWhiteSpace(AppSettings.TranslationAPIUrlGoogle))
                {
                    throw new Exception("Translate API URL is not set.");
                }

                using (var client = TranslationClient.CreateFromApiKey(AppSettings.TranslationAPIKeyGoogle))
                {
                    try
                    {
                        TranslationResult translationResult = await client.TranslateTextAsync(
                                                        text: inputText,
                                                        targetLanguage: targetLang,
                                                        model: TranslationModel.NeuralMachineTranslation);
                        return translationResult.TranslatedText;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"API request failed: {ex.Message}");
                    }
                }
            }
            else
            {
                throw new Exception("Translation API Provider is not set.");
            }
        }
    }
}
