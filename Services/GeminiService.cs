using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NewsImpactRanker.WinForms.Services
{
    public class GeminiService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<string> ClassifyNewsAsync(string text, string apiKey, string modelName, string systemPrompt)
        {
            LogService.Info($"[GEMINI] 🧾 Texto p/ IA - len={text?.Length ?? 0}");

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new Exception("API Key do Gemini não configurada.");

            text = (text ?? "").Trim();
            if (text.Length > 8000) // Gemini aceita mais tokens, podemos ser mais generosos
                text = text.Substring(0, 8000);

            // Se o modelo não vier preenchido, usa o padrão do projeto
            string model = string.IsNullOrWhiteSpace(modelName) ? "gemini-2.5-flash" : modelName;

            if (text.Length > 4000)
                text = text.Substring(0, 4000);

            // Endpoint oficial do Google Generative AI
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var payload = new
            {
                system_instruction = new
                {
                    parts = new[]
                    {
                        new { text = systemPrompt }
                        // new { text = "You are a strict quantitative news impact classifier. Return ONLY a pure JSON object. Keys: impactScore (0-100), impactReason, category." }
                    }
                },
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = "Analyze this news and return the JSON:\n" + text }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    response_mime_type = "application/json" // Força o Gemini a retornar JSON nativo
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            string responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Erro API Gemini: {response.StatusCode} - {responseString}");
            }

            // O Gemini envelopa a resposta. Vamos extrair apenas o texto gerado:
            dynamic jsonResponse = JsonConvert.DeserializeObject(responseString);
            string generatedText = jsonResponse.candidates[0].content.parts[0].text;

            return generatedText;
        }
    }
}