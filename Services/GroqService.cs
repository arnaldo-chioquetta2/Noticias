using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using NewsImpactRanker.WinForms.Models;
using NewsImpactRanker.WinForms.Storage;

namespace NewsImpactRanker.WinForms.Services
{
    public class GroqService
    {
        private readonly HttpClient _httpClient;
        private readonly AppConfig _config;
        private static int _totalTokensToday = 0;
        private static int _totalRequestsToday = 0;
        private static int _totalTokensExecution = 0;
        private static int _totalRequestsExecution = 0;
        private string _lastRawResponse;
        private static readonly SemaphoreSlim _rateLimiter = new SemaphoreSlim(2, 2);

        private const int MAX_TEXT_LENGTH = 3000;
        private const int MAX_TOKENS = 300;

        public GroqService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "NewsImpactRanker/1.0");
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            _config = StorageManager.LoadConfig();
        }

        public async Task<GeminiResponse> ClassifyNewsAsync(string text)
        {
            LogService.Info($"🧾 Texto p/ IA - len={text?.Length ?? 0}");
            LogService.Info("🧾 Texto p/ IA - início: " + (text ?? "").Substring(0, Math.Min(250, (text ?? "").Length)));

            if (string.IsNullOrWhiteSpace(_config.AiApiKey))
                throw new Exception("API Key da Groq não configurada.");

            text = (text ?? "").Trim();

            if (text.Length > 4000)
                text = text.Substring(0, 4000);

            string url = "https://api.groq.com/openai/v1/chat/completions";

            var payload = new
            {
                model = string.IsNullOrWhiteSpace(_config.AiModel) ? "llama-3.1-8b-instant" : _config.AiModel,
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
                "- If research is preliminary, small sample, animal-only, or early-stage, reduce score.\n" +
                "- If impact is speculative, reduce score.\n" +
                "- Score must match the justification logically.\n" +
                "- Never invent facts.\n" +
                "- Never return empty fields.\n\n" +

                "Return ONLY a valid JSON object."
            },
            new {
                role = "user",
                content =
                "Return a JSON object with EXACT keys:\n" +
                "impactScore (integer 0-100), impactReason (string), category (Science, Technology, Health, Politics, Economy, Environment, Other).\n\n" +
                "News:\n" + text
            }
        },
                temperature = 0.3,          // 🔥 AQUI está a diferença
                max_tokens = 800,
                response_format = new { type = "json_object" }
            };

            return await SendRequestAsync(url, JsonConvert.SerializeObject(payload));
        }

        private async Task<GeminiResponse> SendRequestAsync(string url, string jsonPayload)
        {
            try
            {
                return await SendOnceAsync(url, jsonPayload);
            }
            catch (Exception ex)
            {
                if (ex.Message != null && ex.Message.Contains("json_validate_failed"))
                {
                    LogService.Warn("⚠️ Groq json_validate_failed. Reenviando SEM response_format...");

                    var obj = JObject.Parse(jsonPayload);
                    obj.Remove("response_format");

                    return await SendOnceAsync(url, obj.ToString(Formatting.None));
                }

                throw;
            }
        }

        private async Task<GeminiResponse> SendAndTrackAsync(string url, string jsonPayload)
        {
            var response = await SendOnceAsync(url, jsonPayload);

            // 🔥 Aqui capturamos os tokens da última resposta
            try
            {
                var raw = _lastRawResponse;
                // ⚠️ IMPORTANTE:
                // Você precisa garantir que SendOnceAsync armazene a resposta bruta
                // em uma variável privada chamada _lastRawResponse

                if (!string.IsNullOrWhiteSpace(raw))
                {
                    var result = JObject.Parse(raw);

                    var usage = result["usage"];
                    if (usage != null)
                    {
                        int tokens = usage["total_tokens"]?.Value<int>() ?? 0;

                        _totalTokensExecution += tokens;
                        _totalRequestsExecution++;

                        // 🔥 Registra para controle das últimas 24h
                        TokenUsageService.RegisterUsage(tokens);

                        LogService.Debug(
                            $"Tokens usados nesta request: {tokens} | Execução atual: {_totalTokensExecution}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Warn($"Não foi possível capturar usage da resposta: {ex.Message}");
            }

            return response;
        }

        private async Task<GeminiResponse> SendOnceAsync(string url, string jsonPayload)
        {
            using (var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json"))
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = content;
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.AiApiKey);

                var response = await _httpClient.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();
                _lastRawResponse = responseBody;

                if (!response.IsSuccessStatusCode)
                {
                    // joga a msg inteira pra cima (pra detectar json_validate_failed)
                    throw new Exception($"Erro Groq API: {response.StatusCode} - {responseBody}");
                }

                var result = JObject.Parse(responseBody);

                var usage = result["usage"];
                if (usage != null)
                {
                    int tokens = usage["total_tokens"]?.Value<int>() ?? 0;

                    _totalTokensToday += tokens;
                    _totalRequestsToday++;

                    LogService.Debug($"Consumo atual: {_totalTokensToday} tokens em {_totalRequestsToday} requests");
                }

                if (result["choices"]?[0]?["finish_reason"]?.ToString() == "length")
                    throw new Exception("Resposta truncada pelo modelo (max_tokens insuficiente).");

                string textResult = result["choices"]?[0]?["message"]?["content"]?.ToString();
                if (string.IsNullOrWhiteSpace(textResult))
                    throw new Exception("Groq retornou conteúdo vazio.");

                // extrair JSON contando chaves
                int start = textResult.IndexOf('{');
                if (start < 0)
                    throw new Exception("JSON não encontrado na resposta da Groq.");

                int braceCount = 0;
                int end = -1;
                for (int i = start; i < textResult.Length; i++)
                {
                    if (textResult[i] == '{') braceCount++;
                    if (textResult[i] == '}') braceCount--;
                    if (braceCount == 0) { end = i; break; }
                }
                if (end < 0)
                    throw new Exception("JSON incompleto na resposta da Groq.");

                textResult = textResult.Substring(start, end - start + 1)
                                       .Replace("```json", "")
                                       .Replace("```", "")
                                       .Trim();

                LogService.Info("📦 JSON EXTRAÍDO (Groq): " + textResult);

                var obj = JObject.Parse(textResult);

                // exige o contrato
                var impactScoreToken =
                    obj["impactScore"] ??
                    obj.Properties().FirstOrDefault(p => string.Equals(p.Name, "impactScore", StringComparison.OrdinalIgnoreCase))?.Value;

                if (impactScoreToken == null)
                    throw new Exception("JSON fora do contrato (sem impactScore).");

                double score;
                if (!double.TryParse(impactScoreToken.ToString().Replace(",", "."),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out score))
                    score = 0;

                string reason =
                    (obj["impactReason"] ??
                     obj.Properties().FirstOrDefault(p => string.Equals(p.Name, "impactReason", StringComparison.OrdinalIgnoreCase))?.Value)?.ToString();

                string category =
                    (obj["category"] ??
                     obj.Properties().FirstOrDefault(p => string.Equals(p.Name, "category", StringComparison.OrdinalIgnoreCase))?.Value)?.ToString();

                var parsed = new GeminiResponse
                {
                    impactScore = Math.Max(0, Math.Min(100, score)),
                    impactReason = reason,
                    category = category
                };

                LogService.Info($"✅ MAPEADO: Score={parsed.impactScore}, Categoria={parsed.category}, Reason={parsed.impactReason}");
                return parsed;
            }
        }

        public static int GetTotalTokensToday()
        {
            return _totalTokensToday;
        }

        public static int GetTotalRequestsToday()
        {
            return _totalRequestsToday;
        }

    }
}