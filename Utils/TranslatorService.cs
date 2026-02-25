using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace TraducaoTIME.Utils
{
    public class TranslatorService
    {
        private static HttpClient httpClient = new HttpClient();

        public static async Task<string> TraduirTexto(string texto)
        {
            try
            {
                // Obtém credenciais do Azure Translator
                string translatorKey = Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_KEY") ?? "";
                string translatorRegion = Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_REGION") ?? "";

                if (string.IsNullOrWhiteSpace(translatorKey) || string.IsNullOrWhiteSpace(translatorRegion))
                {
                    Console.WriteLine("\n⚠️  Erro: Variáveis AZURE_TRANSLATOR_KEY e AZURE_TRANSLATOR_REGION não configuradas");
                    return texto;
                }

                // Usa REST API do Azure Translator
                string endpoint = $"https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&from=en&to=pt-BR";

                using (var request = new HttpRequestMessage())
                {
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(endpoint);
                    request.Content = new StringContent($"[{{\"Text\": \"{texto}\"}}]", System.Text.Encoding.UTF8, "application/json");
                    request.Headers.Add("Ocp-Apim-Subscription-Key", translatorKey);
                    request.Headers.Add("Ocp-Apim-Subscription-Region", translatorRegion);

                    using (var response = await httpClient.SendAsync(request))
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            return texto;
                        }

                        using (JsonDocument doc = JsonDocument.Parse(responseBody))
                        {
                            var translations = doc.RootElement[0].GetProperty("translations");
                            if (translations.GetArrayLength() > 0)
                            {
                                return translations[0].GetProperty("text").GetString() ?? texto;
                            }
                        }

                        return texto;
                    }
                }
            }
            catch
            {
                return texto;
            }
        }
    }
}
