using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NewsImpactRanker.WinForms.Services
{
    public class GroqService
    {
        // Método principal de classificação
        public async Task<string> ClassifyNewsAsync(string text, string apiKey, string modelName)
        {
            LogService.Info($"[GROQ] 🧾 Texto p/ IA - len={text?.Length ?? 0}");

            // Validação da chave injetada pelo Load Balancer
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new Exception("API Key da Groq não configurada.");

            text = (text ?? "").Trim();

            // Limite de segurança para não estourar o contexto da Groq
            if (text.Length > 4000)
                text = text.Substring(0, 4000);

            string url = "https://api.groq.com/openai/v1/chat/completions";

            var payload = new
            {
                // Usa o modelo passado ou o sucessor do 3.1 que foi desativado
                model = string.IsNullOrWhiteSpace(modelName) ? "llama-3.3-70b-versatile" : modelName,
                messages = new[]
                {
                    new {
                        role = "system",
                        content =
                        "You are a strict quantitative news impact classifier.\n\n" +
                        "Use this scoring scale strictly:\n" +
                        "0–20 = trivial/local news\n" +
                        "21–40 = minor update or small incremental research\n" +
                        "41–60 = moderate impact within a specific field\n" +
                        "61–75 = strong impact in scientific or economic community\n" +
                        "76–85 = major breakthrough or significant global relevance\n" +
                        "86–95 = paradigm-shifting discovery or major global consequence\n" +
                        "96–100 = historic, civilization-level impact\n\n" +
                        "IMPORTANT RULES:\n" +
                        "- Do NOT default to 80.\n" +
                        "- Use the full range realistically.\n" +
                        "- Score must match the justification logically.\n" +
                        "- Return ONLY a valid JSON object."
                    },
                    new {
                        role = "user",
                        content =
                        "Return a JSON object with EXACT keys:\n" +
                        "impactScore (integer 0-100), impactReason (string), category (Science, Technology, Health, Politics, Economy, Environment, Other).\n\n" +
                        "News:\n" + text
                    }
                },
                temperature = 0.3,
                max_tokens = 800,
                response_format = new { type = "json_object" } // Garante que a Groq responda JSON
            };

            // Chamada para o método auxiliar enviando a chave
            return await SendRequestAsync(url, JsonConvert.SerializeObject(payload), apiKey);
        }

        // Método auxiliar que faz a comunicação HTTP
        private async Task<string> SendRequestAsync(string url, string jsonPayload, string apiKey)
        {
            using (var client = new HttpClient())
            {
                // Configura a autenticação Bearer com a chave recebida
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync(url, content);
                    string responseString = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        // Se for erro 429, o Load Balancer no MainForm vai capturar essa Exception e trocar para o Gemini
                        throw new Exception($"Erro Groq API: {response.StatusCode} - {responseString}");
                    }

                    // Extraímos apenas o conteúdo do JSON gerado pela IA dentro da estrutura da Groq/OpenAI
                    dynamic dynamicResponse = JsonConvert.DeserializeObject(responseString);
                    string contentFromAi = dynamicResponse.choices[0].message.content;

                    return contentFromAi;
                }
                catch (Exception ex)
                {
                    LogService.Error($"[GroqService] Falha na comunicação: {ex.Message}");
                    throw; // Repassa para o Failover tratar
                }
            }
        }
    }
}